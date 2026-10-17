# -*- coding: utf-8 -*-
"""Resync identity sequences after ERP sync / manual ID inserts."""
from __future__ import annotations

import os
import sys

import psycopg2

TABLES = ("usuarioecommerce", "usuarioecommercecuenta", "cuenta")


def main() -> int:
    host = os.environ["ECOM_PG_HOST"]
    port = int(os.environ.get("ECOM_PG_PORT", "5432"))
    db = os.environ.get("ECOM_PG_DB", "ecommerce")
    user = os.environ.get("ECOM_PG_USER", "ecom")
    password = os.environ["ECOM_PG_PASSWORD"]

    conn = psycopg2.connect(
        host=host,
        port=port,
        dbname=db,
        user=user,
        password=password,
        sslmode="prefer",
        options="-c search_path=dbo,public",
    )
    conn.autocommit = True
    cur = conn.cursor()
    for table in TABLES:
        sql = (
            f"SELECT setval(pg_get_serial_sequence('dbo.{table}', 'ID'), "
            f'GREATEST((SELECT COALESCE(MAX("ID"), 1) FROM dbo.{table}), 1), true)'
        )
        cur.execute(sql)
        seq_val = cur.fetchone()[0]
        cur.execute(f'SELECT COALESCE(MAX("ID"), 0) FROM dbo.{table}')
        max_id = cur.fetchone()[0]
        print(f"{table}: seq={seq_val} max={max_id}")
    cur.close()
    conn.close()
    return 0


if __name__ == "__main__":
    sys.exit(main())
