"""Sync branding media 205 -> 189 and redeploy API with ErpMedia fix."""
from __future__ import annotations

import os
import subprocess
import sys
import time
from pathlib import Path

import paramiko

PW = os.environ.get("ECOM_DEPLOY_PASSWORD") or os.environ.get("ECOM_ETL_SSH_PASSWORD") or "Dtmx.2025"
ECOM = Path(__file__).resolve().parents[2]


def ssh(host: str) -> paramiko.SSHClient:
    c = paramiko.SSHClient()
    c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    c.connect(host, username="userdb", password=PW, timeout=30)
    return c


def sudo_pty(host: str, cmd: str) -> str:
    c = ssh(host)
    ch = c.get_transport().open_session()
    ch.get_pty()
    ch.exec_command(f"sudo -S {cmd}")
    ch.send(PW + "\n")
    time.sleep(0.5)
    buf = b""
    t0 = time.time()
    while time.time() - t0 < 120:
        if ch.recv_ready():
            buf += ch.recv(8192)
        if ch.exit_status_ready():
            while ch.recv_ready():
                buf += ch.recv(8192)
            break
        time.sleep(0.2)
    c.close()
    return buf.decode("utf-8", "replace")


def main() -> int:
    # 1) tar media on 205 and scp via local
    print("=== pack media on 205 ===")
    print(sudo_pty("10.99.99.205", "tar -C /var/lib/ecommerce -czf /home/userdb/media-tenants.tgz media")[-500:])

    print("=== download media tarball ===")
    c205 = ssh("10.99.99.205")
    sftp = c205.open_sftp()
    local_tar = ECOM / "tools" / "deploy" / "_media-tenants.tgz"
    sftp.get("/home/userdb/media-tenants.tgz", str(local_tar))
    sftp.close()
    c205.close()
    print("downloaded", local_tar, "bytes", local_tar.stat().st_size)

    print("=== upload + extract on 189 ===")
    c189 = ssh("10.99.99.189")
    sftp = c189.open_sftp()
    sftp.put(str(local_tar), "/home/userdb/media-tenants.tgz")
    sftp.close()
    c189.close()
    print(
        sudo_pty(
            "10.99.99.189",
            "mkdir -p /var/lib/ecommerce && tar -C /var/lib/ecommerce -xzf /home/userdb/media-tenants.tgz "
            "&& chown -R userdb:userdb /var/lib/ecommerce && find /var/lib/ecommerce/media -type f | wc -l",
        )[-800:]
    )

    print("=== publish + deploy API ===")
    staging = ECOM / "tools" / "deploy" / "_staging" / "api"
    subprocess.run(
        [
            "dotnet",
            "publish",
            str(ECOM / "src" / "Datamex.ECommerce.Api" / "Datamex.ECommerce.Api.csproj"),
            "-c",
            "Release",
            "-r",
            "linux-x64",
            "--self-contained",
            "true",
            "-o",
            str(staging),
            "/p:PublishSingleFile=false",
        ],
        check=True,
        cwd=ECOM,
    )
    # ensure production settings in staging
    prod = (ECOM / "src/Datamex.ECommerce.Api/appsettings.Production.json").read_text(encoding="utf-8")
    (staging / "appsettings.Production.json").write_text(prod, encoding="utf-8")

    c189 = ssh("10.99.99.189")
    print(sudo_pty("10.99.99.189", "systemctl stop datamex-ecom-api")[-200:])
    sftp = c189.open_sftp()

    def upload_tree(local: Path, remote: str) -> int:
        n = 0
        for p in local.rglob("*"):
            if not p.is_file():
                continue
            rel = p.relative_to(local).as_posix()
            target = f"{remote}/{rel}"
            parent = "/".join(target.split("/")[:-1])
            # mkdir -p via ssh once is better; use sftp mkdir chain
            parts = parent.strip("/").split("/")
            cur = ""
            for part in parts:
                cur += f"/{part}"
                try:
                    sftp.stat(cur)
                except FileNotFoundError:
                    try:
                        sftp.mkdir(cur)
                    except OSError:
                        pass
            sftp.put(str(p), target)
            n += 1
        return n

    n = upload_tree(staging, "/opt/datamex-ecom/api")
    sftp.close()
    print(f"uploaded {n} api files")
    print(sudo_pty("10.99.99.189", "chmod +x /opt/datamex-ecom/api/Datamex.ECommerce.Api && systemctl start datamex-ecom-api")[-300:])
    time.sleep(6)

    _, out, _ = c189.exec_command(
        "curl -fsS http://127.0.0.1/health; echo; "
        "curl -s -o /dev/null -w 'logo=%{http_code}\\n' http://127.0.0.1/media/tenants/41/branding/logo/189c711c881c4928bbd8a4e70842cd94.jpg; "
        "curl -s -o /dev/null -w 'erp=%{http_code}\\n' -H 'X-Tenant-Slug: casa-imperial' http://127.0.0.1/media/erp/8938.JPG"
    )
    print(out.read().decode())
    c189.close()
    try:
        local_tar.unlink()
    except OSError:
        pass
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
