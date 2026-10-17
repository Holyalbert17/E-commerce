# Subir este proyecto a GitHub

El código de la tienda está en esta carpeta. El repositorio git que hoy detecta la herramienta está en el perfil de Windows (`C:\Users\acantero`), no en `E-commerce`, y no tiene commits. No hagas `git add` desde el perfil: subiría el resto de tu usuario.

Creá el repositorio **dentro** de `E-commerce`.

## 1. Qué no debe entrar

`.gitignore` ya excluye:

- `bin/`, `obj/`, `artifacts/`, `dist/`, `node_modules/`, `.angular/`
- carpetas `_run-api*` (copias publicadas de la API)
- `App_Data/` (SQLite local e imágenes subidas)
- `appsettings.json`, `appsettings.Development.json` y `appsettings.Production.json`
- logs y la carpeta de staging de deploy

Esas copias de la API y los compilados se pueden borrar: se vuelven a generar. No son código fuente.

## 2. Secretos

Antes de publicar, buscá en el árbol que no quede una contraseña real:

```powershell
cd E-commerce
git init
git add -A
git status
```

Revisá la lista. No tiene que aparecer:

- `appsettings.json`, `appsettings.Development.json`, `appsettings.Production.json`
- archivos `.db`, carpetas `_run-api`, `bin`, `obj`, `node_modules`
- scripts con una contraseña escrita entre comillas

Los scripts `tools/remote_*.sh` leen la contraseña de la variable `ECOM_PG_PASSWORD`. Definila en la sesión donde los corras; no la vuelvas a dejar en el archivo.

`dotnet ef` lee la conexión de `EcomDbConnection`. Tampoco va una conexión por defecto en el código.

En los environments de producción de Angular hay URLs de ejemplo (`api.ejemplo.local`, `tienda.ejemplo.local`). Poné el dominio real en el servidor de build, o en un archivo que no commitees, antes de generar el build de producción.

Rotá cualquier contraseña que haya estado en archivos de este proyecto, aunque ya la hayas sacado del árbol: pudo quedar en copias locales o en el historial de un editor.

## 3. Primer commit y GitHub

Desde `E-commerce`, con [GitHub CLI](https://cli.github.com/) ya autenticado:

```powershell
cd C:\Users\acantero\Desktop\dev-datamex\E-commerce
git init
git add -A
git status
git commit -m "Documenta la plataforma e-commerce y deja fuera secretos y compilados."
gh repo create datamex-ecommerce --private --source=. --remote=origin --push
```

`--private` es lo adecuado mientras el panel y los scripts de servidor sigan en el mismo repo. Si el remoto ya existe:

```powershell
git remote add origin https://github.com/TU-ORG/TU-REPO.git
git branch -M main
git push -u origin main
```

## 4. Qué va a ver quien clone

1. Lee `README.md`.
2. Copia `appsettings.Example.json` y `appsettings.Development.Example.json` a los nombres sin `Example`.
3. Levanta la API, la tienda y el panel como dice [PUESTA-EN-MARCHA.md](PUESTA-EN-MARCHA.md).
4. Entra a la tienda `demo` con `demo` / `demo123` si la base estaba vacía.

Quien clone no tiene tu PostgreSQL ni tus imágenes. Tiene el código, las migraciones y las plantillas de configuración.
