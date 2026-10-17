# Ecom ETL (único inbound) — 1 ERP → N e-commerces



**App:** [`../../ecom-etl/README.md`](../../ecom-etl/README.md)  

**Checklist:** [`../../ecom-etl/docs/CHECKLIST.md`](../../ecom-etl/docs/CHECKLIST.md)



```powershell

cd ..\..\ecom-etl

.\.venv\Scripts\activate

ecom-etl -c config\local.yaml doctor

ecom-etl -c config\local.yaml once --mode full --tenants multi

ecom-etl -c config\local.yaml continuous --interval 30 --tenants multi

```


