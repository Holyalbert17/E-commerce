r"""Build and deploy Datamex E-Commerce (API + admin + storefront) to a Linux host.

Usage (PowerShell):
  $env:ECOM_DEPLOY_PASSWORD = 'Dtmx.2025'
  python tools/deploy/remote_deploy.py --host 10.99.99.189 --user userdb

Optional: skip build if artifacts already exist:
  python tools/deploy/remote_deploy.py --host 10.99.99.189 --skip-build
"""

from __future__ import annotations

import argparse
import os
import posixpath
import shutil
import subprocess
import sys
from pathlib import Path

import paramiko

ROOT = Path(__file__).resolve().parents[2]
API_PROJECT = ROOT / "src" / "Datamex.ECommerce.Api" / "Datamex.ECommerce.Api.csproj"
ADMIN_DIR = ROOT / "apps" / "admin"
STORE_DIR = ROOT / "apps" / "storefront"
STAGING = ROOT / "tools" / "deploy" / "_staging"
REMOTE_STAGING = "/home/{user}/datamex-ecom-release"
INSTALL_ROOT = "/opt/datamex-ecom"


def run_local(cmd: list[str], *, cwd: Path | None = None) -> None:
    print("+", " ".join(cmd))
    if os.name == "nt" and cmd and cmd[0] in ("npm", "npx"):
        cmd = [cmd[0] + ".cmd", *cmd[1:]]
    subprocess.run(cmd, cwd=cwd or ROOT, check=True)


def build_artifacts(host: str) -> None:
    if STAGING.exists():
        shutil.rmtree(STAGING)
    api_out = STAGING / "api"
    store_out = STAGING / "storefront"
    admin_out = STAGING / "admin"
    api_out.mkdir(parents=True)
    store_out.mkdir(parents=True)
    admin_out.mkdir(parents=True)

    run_local(
        [
            "dotnet",
            "publish",
            str(API_PROJECT),
            "-c",
            "Release",
            "-r",
            "linux-x64",
            "--self-contained",
            "true",
            "-o",
            str(api_out),
            "/p:PublishSingleFile=false",
        ]
    )

    # Patch production URLs if host differs from default in environment.production.ts
    if host != "10.99.99.189":
        for rel in (
            ADMIN_DIR / "src/environments/environment.production.ts",
            STORE_DIR / "src/environments/environment.production.ts",
        ):
            text = rel.read_text(encoding="utf-8")
            text = text.replace("10.99.99.189", host)
            patched = STAGING / rel.relative_to(ROOT)
            patched.parent.mkdir(parents=True, exist_ok=True)
            patched.write_text(text, encoding="utf-8")

    for app_dir, out_dir, name in (
        (STORE_DIR, store_out, "storefront"),
        (ADMIN_DIR, admin_out, "admin"),
    ):
        ng = app_dir / "node_modules" / "@angular" / "cli" / "bin" / "ng.js"
        if not ng.is_file():
            run_local(["npm", "install"], cwd=app_dir)
        build_args = ["node", str(ng), "build", "--configuration", "production"]
        if name == "admin":
            build_args.extend(["--base-href", "/admin/"])
        run_local(build_args, cwd=app_dir)
        dist = app_dir / "dist" / name
        for item in dist.iterdir():
            dest = out_dir / item.name
            if item.is_dir():
                shutil.copytree(item, dest, dirs_exist_ok=True)
            else:
                shutil.copy2(item, dest)

    prod_settings = ROOT / "src/Datamex.ECommerce.Api/appsettings.Production.json"
    api_settings = api_out / "appsettings.Production.json"
    text = prod_settings.read_text(encoding="utf-8")
    if host != "10.99.99.189":
        text = text.replace("10.99.99.189", host)
    api_settings.write_text(text, encoding="utf-8")


def upload_tree(sftp: paramiko.SFTPClient, local_root: Path, remote_root: str) -> int:
    copied = 0
    for source in local_root.rglob("*"):
        if not source.is_file():
            continue
        rel = source.relative_to(local_root).as_posix()
        target = posixpath.join(remote_root, rel)
        parent = posixpath.dirname(target)
        parts = parent.strip("/").split("/")
        cur = ""
        for part in parts:
            cur += f"/{part}"
            try:
                sftp.stat(cur)
            except FileNotFoundError:
                sftp.mkdir(cur)
        sftp.put(str(source), target)
        copied += 1
    return copied


def ssh_run(client: paramiko.SSHClient, password: str, command: str, *, sudo: bool = False) -> tuple[int, str]:
    _, stdout, stderr = client.exec_command(command, get_pty=sudo)
    if sudo:
        stdin = stdout.channel.makefile_stdin("wb")
        stdin.write((password + "\n").encode())
        stdin.flush()
    out = (stdout.read() + stderr.read()).decode("utf-8", "replace")
    return stdout.channel.recv_exit_status(), out


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="10.99.99.189")
    parser.add_argument("--user", default="userdb")
    parser.add_argument("--skip-build", action="store_true")
    args = parser.parse_args()
    password = os.environ.get("ECOM_DEPLOY_PASSWORD") or os.environ.get("ECOM_ETL_SSH_PASSWORD")
    if not password:
        parser.error("Definí ECOM_DEPLOY_PASSWORD o ECOM_ETL_SSH_PASSWORD.")

    if not args.skip_build:
        build_artifacts(args.host)
    elif not STAGING.is_dir():
        parser.error("No hay staging; ejecutá sin --skip-build primero.")

    client = paramiko.SSHClient()
    client.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    client.connect(args.host, username=args.user, password=password, timeout=30)

    remote_stage = REMOTE_STAGING.format(user=args.user)
    ssh_run(client, password, f"rm -rf {remote_stage} && mkdir -p {remote_stage}")
    sftp = client.open_sftp()
    copied = upload_tree(sftp, STAGING, remote_stage)
    sftp.put(str(Path(__file__).with_name("install-remote.sh")), f"{remote_stage}/install-remote.sh")
    sftp.close()
    print(f"Uploaded {copied} files to {remote_stage}")

    install = (
        f"sudo -S bash -lc "
        f"'sed -i \"s/\\r$//\" {remote_stage}/install-remote.sh && "
        f"rm -rf {INSTALL_ROOT} && mkdir -p {INSTALL_ROOT} && "
        f"cp -a {remote_stage}/api {INSTALL_ROOT}/ && "
        f"cp -a {remote_stage}/storefront {INSTALL_ROOT}/ && "
        f"cp -a {remote_stage}/admin {INSTALL_ROOT}/ && "
        f"chmod +x {INSTALL_ROOT}/api/Datamex.ECommerce.Api && "
        f"DEPLOY_USER={args.user} bash {remote_stage}/install-remote.sh'"
    )
    status, output = ssh_run(client, password, install, sudo=True)
    print(output.encode("ascii", "backslashreplace").decode())
    if status:
        client.close()
        raise SystemExit(status)

    smoke_cmds = [
        f"curl -fsS http://127.0.0.1:5082/health",
        f"curl -fsSI http://127.0.0.1:4200/ | head -n 1",
        f"curl -fsSI http://127.0.0.1:4201/ | head -n 1",
    ]
    for cmd in smoke_cmds:
        rc, out = ssh_run(client, password, cmd)
        print(out.strip())

    client.close()
    print(f"\nDeploy OK")
    print(f"  Vitrina:    http://{args.host}/?tenant=casa-imperial")
    print(f"  Admin:      http://{args.host}/admin/")
    print(f"  API/Swagger http://{args.host}/swagger")
    print(f"  Health:     http://{args.host}/health")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
