# -*- coding: utf-8 -*-
"""Helpers to manage ecommerce Postgres on Patroni host via SSH/SFTP."""
from __future__ import annotations

import pathlib
import sys

import paramiko

sys.stdout.reconfigure(encoding="utf-8", errors="replace")

HOST = "10.99.99.205"
USER = "userdb"
PASS = "Dtmx.2025"
REMOTE_DIR = "/tmp/ecom-pg-setup"


def connect() -> paramiko.SSHClient:
    c = paramiko.SSHClient()
    c.set_missing_host_key_policy(paramiko.AutoAddPolicy())
    c.connect(HOST, username=USER, password=PASS, timeout=30, allow_agent=False, look_for_keys=False)
    return c


def upload(local: pathlib.Path, remote_name: str) -> None:
    c = connect()
    try:
        sftp = c.open_sftp()
        try:
            try:
                sftp.mkdir(REMOTE_DIR)
            except OSError:
                pass
            sftp.put(str(local), f"{REMOTE_DIR}/{remote_name}")
            sftp.chmod(f"{REMOTE_DIR}/{remote_name}", 0o755)
        finally:
            sftp.close()
    finally:
        c.close()


def run_sudo_script(remote_name: str) -> str:
    c = connect()
    try:
        cmd = f"echo '{PASS}' | sudo -S bash {REMOTE_DIR}/{remote_name}"
        _, stdout, stderr = c.exec_command(cmd, get_pty=True)
        out = stdout.read().decode("utf-8", "replace")
        err = stderr.read().decode("utf-8", "replace")
        return out + (("\nERR:\n" + err) if err.strip() else "")
    finally:
        c.close()


def main() -> None:
    action = sys.argv[1] if len(sys.argv) > 1 else "status"
    tools = pathlib.Path(__file__).resolve().parent
    mapping = {
        "status": "remote_status.sh",
        "configure": "remote_configure_ecommerce.sh",
        "diag": "remote_diag.sh",
        "install-standalone": "remote_install_standalone_ecom.sh",
        "verify": "remote_verify_ecom_db.sh",
        "apply-schema": "remote_apply_schema.sh",
        "wipe-dbo": "remote_wipe_dbo.sh",
        "diag-ports": "remote_diag_ports.sh",
        "move-5432": "remote_move_ecom_5432.sh",
        "apply-parity": "remote_apply_parity.sh",
        "sync-progress": "remote_sync_progress.sh",
        "price-counts": "remote_price_counts.sh",
        "media-setup": "remote_media_setup.sh",
    }
    if action not in mapping:
        raise SystemExit(f"Unknown action: {action}. Use: {', '.join(mapping)}")
    script = mapping[action]
    # normalize LF before upload
    p = tools / script
    p.write_bytes(p.read_bytes().replace(b"\r\n", b"\n").replace(b"\r", b"\n"))
    upload(p, script)
    if action == "apply-schema":
        sql = tools / "ecom_schema_dbo.sql"
        if not sql.exists():
            raise SystemExit("Missing tools/ecom_schema_dbo.sql — generate with dotnet ef migrations script")
        sql.write_bytes(sql.read_bytes().replace(b"\r\n", b"\n").replace(b"\r", b"\n"))
        upload(sql, "ecom_schema_dbo.sql")
    if action == "apply-parity":
        sql = tools / "ecom_empresa_parity.sql"
        if not sql.exists():
            raise SystemExit("Missing tools/ecom_empresa_parity.sql")
        sql.write_bytes(sql.read_bytes().replace(b"\r\n", b"\n").replace(b"\r", b"\n"))
        upload(sql, "ecom_empresa_parity.sql")
    print(run_sudo_script(script))


if __name__ == "__main__":
    main()
