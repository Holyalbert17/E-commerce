import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AdminApiService,
  AdminFuncionalidadDto,
  AdminRoleDto,
  AdminUserDto,
  AdminUserPermissionTreeDto,
} from '../core/admin-api.service';
import { PermissionTreeComponent, PermissionTreeNode } from '../shared/permission-tree.component';

type ManagerTab = 'roles' | 'usuarios' | 'catalogo';
type RolePanel = 'list' | 'create' | 'edit' | 'permisos' | 'copiar';
type UserPanel = 'list' | 'create' | 'edit' | 'roles' | 'operaciones';

@Component({
  standalone: true,
  imports: [FormsModule, DatePipe, PermissionTreeComponent],
  template: `
    <div class="dx-toolbar">
      <div>
        <h1 class="dx-page-title">Seguridad — Manager</h1>
        <p class="dx-page-sub">
          Paridad ERP: EmpresaRol · EmpresaUsuario · Operaciones ·
          <strong>TreeEmpresaUsuarioOperacion</strong> (grants por rol ∪ directos).
        </p>
      </div>
      <button type="button" class="btn btn-outline-secondary btn-sm" (click)="reload()">
        <i class="fas fa-sync-alt mr-1"></i> Recargar
      </button>
    </div>

    @if (error()) {
      <div class="alert alert-danger alert-dismissible">
        {{ error() }}
        <button type="button" class="close" (click)="error.set(null)"><span>&times;</span></button>
      </div>
    }
    @if (ok()) {
      <div class="alert alert-success alert-dismissible">
        {{ ok() }}
        <button type="button" class="close" (click)="ok.set(null)"><span>&times;</span></button>
      </div>
    }

    <ul class="nav nav-tabs dx-erp-tabs mb-3">
      <li class="nav-item">
        <a class="nav-link" href="#" [class.active]="tab === 'roles'" (click)="setTab('roles', $event)">
          <i class="fas fa-user-tag mr-1"></i> Roles
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link" href="#" [class.active]="tab === 'usuarios'" (click)="setTab('usuarios', $event)">
          <i class="fas fa-users mr-1"></i> Usuarios
        </a>
      </li>
      <li class="nav-item">
        <a class="nav-link" href="#" [class.active]="tab === 'catalogo'" (click)="setTab('catalogo', $event)">
          <i class="fas fa-key mr-1"></i> Catálogo operaciones
        </a>
      </li>
    </ul>

    @if (tab === 'roles') {
      @if (rolePanel === 'list') {
        <div class="card card-outline card-primary">
          <div class="card-header">
            <h3 class="card-title">EmpresaRoles</h3>
            <div class="card-tools">
              <div class="input-group input-group-sm d-inline-flex mr-2" style="width:200px">
                <input class="form-control" placeholder="Filtrar…" [(ngModel)]="roleFilter" />
              </div>
              <button type="button" class="btn btn-sm btn-info" (click)="openCreateRole()">
                <i class="fas fa-plus mr-1"></i> Nuevo
              </button>
            </div>
          </div>
          <div class="card-body p-0 table-responsive">
            <table class="table table-striped table-hover table-bordered mb-0">
              <thead>
                <tr>
                  <th style="width:120px">Código</th>
                  <th>Nombre</th>
                  <th style="width:90px">Activo</th>
                  <th style="width:100px">Permisos</th>
                  <th style="width:150px">Últ. modif.</th>
                  <th style="width:180px" class="text-center">Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (r of filteredRoles(); track r.id) {
                  <tr [class.table-secondary]="!r.activo">
                    <td><code>{{ r.codigoRol }}</code></td>
                    <td>{{ r.nombreRol }}</td>
                    <td class="text-center">
                      @if (r.activo) {
                        <span class="badge badge-soft-success">Sí</span>
                      } @else {
                        <span class="badge badge-soft-danger">No</span>
                      }
                    </td>
                    <td class="text-center">
                      <span class="badge badge-primary">{{ r.permisosCount }}</span>
                    </td>
                    <td class="text-muted small">{{ r.ultModificacion | date: 'short' }}</td>
                    <td class="table-actions text-center text-nowrap">
                      <button type="button" class="btn btn-xs btn-default" title="Editar" (click)="openEditRole(r)">
                        <i class="fas fa-edit"></i>
                      </button>
                      <button type="button" class="btn btn-xs btn-primary" title="Permisos (árbol)"
                              (click)="openPermisos(r)" [disabled]="!r.activo">
                        <i class="fas fa-sitemap"></i>
                      </button>
                      <button type="button" class="btn btn-xs btn-secondary" title="Copiar permisos"
                              (click)="openCopiar(r)" [disabled]="!r.activo">
                        <i class="fas fa-copy"></i>
                      </button>
                      <button type="button" class="btn btn-xs btn-danger" title="Eliminar"
                              (click)="deleteRole(r)" [disabled]="!r.activo || r.codigoRol === 'SUPER'">
                        <i class="fas fa-trash"></i>
                      </button>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="6">
                      <div class="dx-empty py-4">
                        <i class="fas fa-user-shield"></i>
                        <p>Sin roles definidos.</p>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      @if (rolePanel === 'create' || rolePanel === 'edit') {
        <div class="card card-outline card-info">
          <div class="card-header">
            <h3 class="card-title">{{ rolePanel === 'create' ? 'Crear rol' : 'Editar rol' }}</h3>
            <div class="card-tools">
              <ol class="breadcrumb float-sm-right mb-0 bg-transparent p-0">
                <li class="breadcrumb-item"><a href="#" (click)="backRoles($event)">Roles</a></li>
                <li class="breadcrumb-item active">{{ rolePanel === 'create' ? 'Nuevo' : 'Editar' }}</li>
              </ol>
            </div>
          </div>
          <div class="card-body">
            <form class="form-horizontal dx-erp-form" (ngSubmit)="saveRole()">
              <div class="form-group row">
                <label class="col-sm-3 col-form-label">Código</label>
                <div class="col-sm-6">
                  <input class="form-control" [(ngModel)]="roleForm.codigoRol" name="codigoRol"
                         [readonly]="rolePanel === 'edit'" required />
                </div>
              </div>
              <div class="form-group row">
                <label class="col-sm-3 col-form-label">Nombre</label>
                <div class="col-sm-6">
                  <input class="form-control" [(ngModel)]="roleForm.nombreRol" name="nombreRol" required />
                </div>
              </div>
              @if (rolePanel === 'edit') {
                <div class="form-group row">
                  <label class="col-sm-3 col-form-label">Activo</label>
                  <div class="col-sm-6">
                    <div class="custom-control custom-switch mt-2">
                      <input type="checkbox" class="custom-control-input" id="roleActivo"
                             [(ngModel)]="roleForm.activo" name="activo" />
                      <label class="custom-control-label" for="roleActivo"></label>
                    </div>
                  </div>
                </div>
              }
              <div class="form-group row mb-0">
                <div class="col-sm-9 offset-sm-3">
                  <button type="button" class="btn btn-info btn-sm mr-2" (click)="backRoles($event)">
                    <i class="fas fa-arrow-left mr-1"></i> Volver
                  </button>
                  <button type="submit" class="btn btn-primary btn-sm">
                    <i class="fas fa-save mr-1"></i> Guardar
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      }

      @if (rolePanel === 'permisos' && selectedRole) {
        <div class="card card-outline card-primary">
          <div class="card-header">
            <h3 class="card-title">
              Operaciones del rol
              <small class="text-muted ml-2">{{ selectedRole.codigoRol }} — {{ selectedRole.nombreRol }}</small>
            </h3>
            <div class="card-tools">
              <ol class="breadcrumb float-sm-right mb-0 bg-transparent p-0">
                <li class="breadcrumb-item"><a href="#" (click)="backRoles($event)">Roles</a></li>
                <li class="breadcrumb-item active">Permisos</li>
              </ol>
            </div>
          </div>
          <div class="card-body">
            <div class="dx-tree-toolbar mb-3">
              <div class="input-group input-group-sm" style="max-width:280px">
                <div class="input-group-prepend">
                  <span class="input-group-text"><i class="fas fa-filter"></i></span>
                </div>
                <input class="form-control" placeholder="Filtrar operaciones…" [(ngModel)]="permFilter" />
              </div>
              <div>
                <button type="button" class="btn btn-xs btn-default mr-1" (click)="selectAllPerms(true)">
                  Seleccionar todo
                </button>
                <button type="button" class="btn btn-xs btn-default" (click)="selectAllPerms(false)">
                  Deseleccionar
                </button>
              </div>
            </div>

            <div class="dx-perm-tree">
              @for (group of permissionGroups(); track group.key) {
                <div class="dx-perm-group">
                      <div class="dx-perm-group-title">
                    <div class="custom-control custom-checkbox">
                      <input type="checkbox" class="custom-control-input" [id]="'grp-' + group.key"
                             [checked]="groupAllSelected(group)"
                             (change)="toggleGroup(group, $event)" />
                      <label class="custom-control-label font-weight-bold" [attr.for]="'grp-' + group.key">
                        {{ group.label }}
                        @if (groupPartial(group)) {
                          <span class="badge badge-warning ml-1">parcial</span>
                        }
                        <span class="badge badge-light ml-1">{{ group.items.length }}</span>
                      </label>
                    </div>
                  </div>
                  <div class="dx-perm-group-body">
                    @for (f of group.items; track f.id) {
                      <div class="custom-control custom-checkbox dx-perm-item">
                        <input type="checkbox" class="custom-control-input" [id]="'perm-' + f.id"
                               [checked]="selectedPerms.has(f.codigoPermission)"
                               (change)="togglePerm(f.codigoPermission, $event)" />
                        <label class="custom-control-label" [attr.for]="'perm-' + f.id">
                          <code>{{ f.codigoPermission }}</code>
                          <small class="text-muted ml-2">{{ f.descripcion }}</small>
                        </label>
                      </div>
                    }
                  </div>
                </div>
              }
            </div>
          </div>
          <div class="card-footer">
            <button type="button" class="btn btn-info btn-sm mr-2" (click)="backRoles($event)">
              <i class="fas fa-arrow-left mr-1"></i> Volver
            </button>
            <button type="button" class="btn btn-primary btn-sm" (click)="savePermisos()">
              <i class="fas fa-save mr-1"></i> Guardar árbol
            </button>
            <span class="text-muted ml-3 small">{{ selectedPerms.size }} operaciones seleccionadas</span>
          </div>
        </div>
      }

      @if (rolePanel === 'copiar' && selectedRole) {
        <div class="card card-outline card-secondary">
          <div class="card-header">
            <h3 class="card-title">Copiar operaciones → {{ selectedRole.codigoRol }}</h3>
          </div>
          <div class="card-body">
            <div class="form-group row">
              <label class="col-sm-3 col-form-label">Rol origen</label>
              <div class="col-sm-6">
                <select class="custom-select" [(ngModel)]="copyFromRoleId">
                  @for (r of rolesActivosExcept(selectedRole.id); track r.id) {
                    <option [ngValue]="r.id">{{ r.codigoRol }} — {{ r.nombreRol }} ({{ r.permisosCount }})</option>
                  }
                </select>
              </div>
            </div>
          </div>
          <div class="card-footer">
            <button type="button" class="btn btn-info btn-sm mr-2" (click)="backRoles($event)">Volver</button>
            <button type="button" class="btn btn-primary btn-sm" (click)="copyPermisos()"
                    [disabled]="!copyFromRoleId">
              <i class="fas fa-copy mr-1"></i> Copiar árbol
            </button>
          </div>
        </div>
      }
    }

    @if (tab === 'usuarios') {
      @if (userPanel === 'list') {
        <div class="card card-outline card-warning">
          <div class="card-header">
            <h3 class="card-title">EmpresaUsuarios</h3>
            <div class="card-tools">
              <div class="input-group input-group-sm d-inline-flex mr-2" style="width:200px">
                <input class="form-control" placeholder="Filtrar…" [(ngModel)]="userFilter" />
              </div>
              <button type="button" class="btn btn-sm btn-info" (click)="openCreateUser()">
                <i class="fas fa-plus mr-1"></i> Nuevo
              </button>
            </div>
          </div>
          <div class="card-body p-0 table-responsive">
            <table class="table table-striped table-hover table-bordered mb-0">
              <thead>
                <tr>
                  <th style="width:130px">Usuario</th>
                  <th>Nombre</th>
                  <th>Email</th>
                  <th>Roles</th>
                  <th style="width:80px">Activo</th>
                  <th style="width:150px">Últ. modif.</th>
                  <th style="width:150px" class="text-center">Acciones</th>
                </tr>
              </thead>
              <tbody>
                @for (u of filteredUsers(); track u.id) {
                  <tr [class.table-secondary]="!u.activo">
                    <td><strong>{{ u.usuario }}</strong></td>
                    <td>{{ u.nombre || '—' }}</td>
                    <td>{{ u.email || '—' }}</td>
                    <td>
                      @for (r of u.roles; track r.idRol) {
                        <span class="badge badge-info mr-1">{{ r.codigoRol }}</span>
                      } @empty {
                        <span class="text-muted">—</span>
                      }
                    </td>
                    <td class="text-center">
                      @if (u.activo) {
                        <span class="badge badge-soft-success">Sí</span>
                      } @else {
                        <span class="badge badge-soft-danger">No</span>
                      }
                    </td>
                    <td class="text-muted small">{{ u.ultModificacion | date: 'short' }}</td>
                    <td class="table-actions text-center text-nowrap">
                      <button type="button" class="btn btn-xs btn-default" title="Editar" (click)="openEditUser(u)">
                        <i class="fas fa-edit"></i>
                      </button>
                      <button type="button" class="btn btn-xs btn-warning" title="Asignar roles"
                              (click)="openUserRoles(u)" [disabled]="!u.activo">
                        <i class="fas fa-user-tag"></i>
                      </button>
                      <button type="button" class="btn btn-xs btn-primary" title="Operaciones (árbol ERP)"
                              (click)="openUserOps(u)" [disabled]="!u.activo">
                        <i class="fas fa-sitemap"></i>
                      </button>
                      <button type="button" class="btn btn-xs btn-danger" title="Eliminar"
                              (click)="deleteUser(u)" [disabled]="!u.activo || u.usuario === 'admin'">
                        <i class="fas fa-trash"></i>
                      </button>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="7">
                      <div class="dx-empty py-4">
                        <i class="fas fa-users-cog"></i>
                        <p>Sin usuarios admin.</p>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      }

      @if (userPanel === 'create' || userPanel === 'edit') {
        <div class="card card-outline card-info">
          <div class="card-header">
            <h3 class="card-title">{{ userPanel === 'create' ? 'Crear usuario' : 'Editar usuario' }}</h3>
          </div>
          <div class="card-body">
            <form class="form-horizontal dx-erp-form" (ngSubmit)="saveUser()">
              <div class="form-group row">
                <label class="col-sm-3 col-form-label">Usuario</label>
                <div class="col-sm-6">
                  <input class="form-control" [(ngModel)]="userForm.usuario" name="usuario"
                         [readonly]="userPanel === 'edit'" required />
                </div>
              </div>
              <div class="form-group row">
                <label class="col-sm-3 col-form-label">
                  {{ userPanel === 'create' ? 'Password' : 'Nueva password' }}
                </label>
                <div class="col-sm-6">
                  <input class="form-control" type="password" [(ngModel)]="userForm.password" name="password"
                         [required]="userPanel === 'create'"
                         [placeholder]="userPanel === 'edit' ? '(sin cambio)' : ''" />
                </div>
              </div>
              <div class="form-group row">
                <label class="col-sm-3 col-form-label">Nombre</label>
                <div class="col-sm-6">
                  <input class="form-control" [(ngModel)]="userForm.nombre" name="nombre" />
                </div>
              </div>
              <div class="form-group row">
                <label class="col-sm-3 col-form-label">Email</label>
                <div class="col-sm-6">
                  <input class="form-control" type="email" [(ngModel)]="userForm.email" name="email" />
                </div>
              </div>
              @if (userPanel === 'edit') {
                <div class="form-group row">
                  <label class="col-sm-3 col-form-label">Activo</label>
                  <div class="col-sm-6">
                    <div class="custom-control custom-switch mt-2">
                      <input type="checkbox" class="custom-control-input" id="userActivo"
                             [(ngModel)]="userForm.activo" name="activo" />
                      <label class="custom-control-label" for="userActivo"></label>
                    </div>
                  </div>
                </div>
              }
              <div class="form-group row mb-0">
                <div class="col-sm-9 offset-sm-3">
                  <button type="button" class="btn btn-info btn-sm mr-2" (click)="backUsers($event)">
                    <i class="fas fa-arrow-left mr-1"></i> Volver
                  </button>
                  <button type="submit" class="btn btn-primary btn-sm">
                    <i class="fas fa-save mr-1"></i> Guardar
                  </button>
                </div>
              </div>
            </form>
          </div>
        </div>
      }

      @if (userPanel === 'roles' && selectedUser) {
        <div class="card card-outline card-warning">
          <div class="card-header">
            <h3 class="card-title">
              EmpresaUsuarioRol
              <small class="text-muted ml-2">{{ selectedUser.usuario }}</small>
            </h3>
          </div>
          <div class="card-body">
            <div class="form-row align-items-end mb-3">
              <div class="col-md-6 form-group mb-0">
                <label>Asignar rol</label>
                <select class="custom-select" [(ngModel)]="assignRoleId">
                  @for (r of rolesActivos(); track r.id) {
                    <option [ngValue]="r.id">{{ r.codigoRol }} — {{ r.nombreRol }}</option>
                  }
                </select>
              </div>
              <div class="col-md-3">
                <button type="button" class="btn btn-warning btn-sm" (click)="assignRole()"
                        [disabled]="!assignRoleId">
                  <i class="fas fa-plus mr-1"></i> Asignar
                </button>
              </div>
            </div>
            <table class="table table-bordered table-sm mb-0">
              <thead>
                <tr>
                  <th>Código</th>
                  <th>Nombre</th>
                  <th style="width:80px"></th>
                </tr>
              </thead>
              <tbody>
                @for (r of selectedUser.roles; track r.idRol) {
                  <tr>
                    <td><code>{{ r.codigoRol }}</code></td>
                    <td>{{ r.nombreRol }}</td>
                    <td class="text-center">
                      <button type="button" class="btn btn-xs btn-outline-danger"
                              (click)="unassignRole(r.idRol)">
                        <i class="fas fa-times"></i>
                      </button>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="3">
                      <div class="dx-empty py-3">
                        <p>Sin roles asignados.</p>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="card-footer">
            <button type="button" class="btn btn-info btn-sm" (click)="backUsers($event)">
              <i class="fas fa-arrow-left mr-1"></i> Volver
            </button>
          </div>
        </div>
      }

      @if (userPanel === 'operaciones' && selectedUser) {
        <div class="card card-outline card-primary">
          <div class="card-header">
            <h3 class="card-title">
              <i class="fas fa-sitemap mr-1"></i>
              Operaciones — {{ selectedUser.usuario }}
            </h3>
            <div class="card-tools">
              @for (r of userOps?.roleCodes || []; track r) {
                <span class="badge badge-success mr-1">rol {{ r }}</span>
              }
            </div>
          </div>
          <div class="card-body">
            <p class="text-muted small">
              Igual ERP <code>TreeEmpresaUsuarioOperacion</code>: los permisos en verde vienen del rol y no se
              pueden desmarcar aquí. Los checkboxes son grants directos
              (<code>empresausuariooperacion</code> / <code>adminusuariofuncionalidad</code>).
            </p>
            <app-permission-tree
              [nodes]="userOpsTree"
              (selectionChange)="onUserOpsSelection($event)"
            />
          </div>
          <div class="card-footer">
            <button type="button" class="btn btn-info btn-sm mr-2" (click)="backUsers($event)">
              <i class="fas fa-arrow-left mr-1"></i> Volver
            </button>
            <button type="button" class="btn btn-success btn-sm" (click)="saveUserOps()" [disabled]="busyOps">
              <i class="fas fa-save mr-1"></i> Guardar árbol
            </button>
          </div>
        </div>
      }
    }

    @if (tab === 'catalogo') {
      <div class="card card-outline card-secondary">
        <div class="card-header">
          <h3 class="card-title">Catálogo PermissionCodes</h3>
          <div class="card-tools">
            <input class="form-control form-control-sm" style="width:220px" placeholder="Buscar…"
                   [(ngModel)]="catalogFilter" />
          </div>
        </div>
        <div class="card-body p-0 table-responsive" style="max-height:560px;overflow:auto">
          <table class="table table-sm table-striped table-hover mb-0">
            <thead>
              <tr>
                <th style="width:40%">Código</th>
                <th>Descripción</th>
                <th style="width:140px">Grupo</th>
              </tr>
            </thead>
            <tbody>
              @for (f of filteredCatalog(); track f.id) {
                <tr>
                  <td><code>{{ f.codigoPermission }}</code></td>
                  <td>{{ f.descripcion }}</td>
                  <td><span class="badge badge-light">{{ permGroupKey(f.codigoPermission) }}</span></td>
                </tr>
              } @empty {
                <tr>
                  <td colspan="3">
                    <div class="dx-empty py-3">
                      <i class="fas fa-key"></i>
                      <p>Sin resultados de permisos.</p>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      </div>
    }
  `,
})
export class ManagerPageComponent {
  private readonly api = inject(AdminApiService);

  tab: ManagerTab = 'roles';
  rolePanel: RolePanel = 'list';
  userPanel: UserPanel = 'list';

  readonly roles = signal<AdminRoleDto[]>([]);
  readonly users = signal<AdminUserDto[]>([]);
  readonly functionalities = signal<AdminFuncionalidadDto[]>([]);
  readonly error = signal<string | null>(null);
  readonly ok = signal<string | null>(null);

  roleFilter = '';
  userFilter = '';
  permFilter = '';
  catalogFilter = '';

  selectedRole: AdminRoleDto | null = null;
  selectedUser: AdminUserDto | null = null;
  selectedPerms = new Set<string>();
  copyFromRoleId: number | null = null;
  assignRoleId: number | null = null;

  userOps: AdminUserPermissionTreeDto | null = null;
  userOpsTree: PermissionTreeNode[] = [];
  userOpsSelectedKeys: string[] = [];
  busyOps = false;

  roleForm = { id: 0, codigoRol: '', nombreRol: '', activo: true };
  userForm = { id: 0, usuario: '', password: '', nombre: '', email: '', activo: true };

  constructor() {
    this.reload();
  }

  setTab(t: ManagerTab, ev: Event): void {
    ev.preventDefault();
    this.tab = t;
    this.rolePanel = 'list';
    this.userPanel = 'list';
  }

  filteredRoles(): AdminRoleDto[] {
    const q = this.roleFilter.trim().toLowerCase();
    if (!q) return this.roles();
    return this.roles().filter(
      (r) => r.codigoRol.toLowerCase().includes(q) || r.nombreRol.toLowerCase().includes(q),
    );
  }

  filteredUsers(): AdminUserDto[] {
    const q = this.userFilter.trim().toLowerCase();
    if (!q) return this.users();
    return this.users().filter(
      (u) =>
        u.usuario.toLowerCase().includes(q) ||
        (u.nombre || '').toLowerCase().includes(q) ||
        (u.email || '').toLowerCase().includes(q),
    );
  }

  filteredCatalog(): AdminFuncionalidadDto[] {
    const q = this.catalogFilter.trim().toLowerCase();
    if (!q) return this.functionalities();
    return this.functionalities().filter(
      (f) =>
        f.codigoPermission.toLowerCase().includes(q) ||
        (f.descripcion || '').toLowerCase().includes(q),
    );
  }

  rolesActivos(): AdminRoleDto[] {
    return this.roles().filter((r) => r.activo);
  }

  rolesActivosExcept(id: number): AdminRoleDto[] {
    return this.roles().filter((r) => r.activo && r.id !== id);
  }

  permGroupKey(code: string): string {
    if (code.startsWith('manager:')) return 'Manager';
    if (code.startsWith('Site_App_Ecommerce_Empresa')) return 'Empresa';
    if (code.startsWith('Site_App_Ecommerce_Productos')) return 'Productos';
    if (code.startsWith('Site_App_Ecommerce_ListasPrecios')) return 'Listas precios';
    if (code.startsWith('Site_App_Ecommerce_TarifaDelivery')) return 'Tarifas';
    if (code.startsWith('Site_App_Ecommerce_Clientes')) return 'Clientes';
    if (code.startsWith('Site_App_Ecommerce_Carrito')) return 'Carritos';
    if (code.startsWith('Site_App_Ecommerce_GrupoArticulo')) return 'Promociones';
    if (code.startsWith('Site_App_Ecommerce_Pedidos')) return 'Pedidos';
    if (code.startsWith('Site_App_Ecommerce')) return 'E-Commerce';
    const i = code.indexOf('_');
    return i > 0 ? code.slice(0, i) : 'Otros';
  }

  permissionGroups(): { key: string; label: string; items: AdminFuncionalidadDto[] }[] {
    const q = this.permFilter.trim().toLowerCase();
    const items = this.functionalities().filter(
      (f) =>
        !q ||
        f.codigoPermission.toLowerCase().includes(q) ||
        (f.descripcion || '').toLowerCase().includes(q),
    );
    const map = new Map<string, AdminFuncionalidadDto[]>();
    for (const f of items) {
      const key = this.permGroupKey(f.codigoPermission);
      if (!map.has(key)) map.set(key, []);
      map.get(key)!.push(f);
    }
    return [...map.entries()]
      .sort((a, b) => a[0].localeCompare(b[0]))
      .map(([key, groupItems]) => ({ key, label: key, items: groupItems }));
  }

  groupAllSelected(group: { items: AdminFuncionalidadDto[] }): boolean {
    return group.items.length > 0 && group.items.every((f) => this.selectedPerms.has(f.codigoPermission));
  }

  groupPartial(group: { items: AdminFuncionalidadDto[] }): boolean {
    const n = group.items.filter((f) => this.selectedPerms.has(f.codigoPermission)).length;
    return n > 0 && n < group.items.length;
  }

  toggleGroup(group: { items: AdminFuncionalidadDto[] }, ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    for (const f of group.items) {
      if (checked) this.selectedPerms.add(f.codigoPermission);
      else this.selectedPerms.delete(f.codigoPermission);
    }
  }

  togglePerm(code: string, ev: Event): void {
    const checked = (ev.target as HTMLInputElement).checked;
    if (checked) this.selectedPerms.add(code);
    else this.selectedPerms.delete(code);
  }

  selectAllPerms(on: boolean): void {
    for (const group of this.permissionGroups()) {
      for (const f of group.items) {
        if (on) this.selectedPerms.add(f.codigoPermission);
        else this.selectedPerms.delete(f.codigoPermission);
      }
    }
  }

  reload(): void {
    this.error.set(null);
    this.api.listRoles(true).subscribe({
      next: (r) => this.roles.set(r),
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
    this.api.listUsers(true).subscribe({
      next: (u) => {
        this.users.set(u);
        if (this.selectedUser) {
          this.selectedUser = u.find((x) => x.id === this.selectedUser!.id) ?? this.selectedUser;
        }
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
    this.api.listFunctionalities().subscribe({
      next: (f) => this.functionalities.set(f),
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  backRoles(ev?: Event): void {
    ev?.preventDefault();
    this.rolePanel = 'list';
    this.selectedRole = null;
  }

  backUsers(ev?: Event): void {
    ev?.preventDefault();
    this.userPanel = 'list';
    this.selectedUser = null;
  }

  openCreateRole(): void {
    this.roleForm = { id: 0, codigoRol: '', nombreRol: '', activo: true };
    this.rolePanel = 'create';
  }

  openEditRole(r: AdminRoleDto): void {
    this.roleForm = { id: r.id, codigoRol: r.codigoRol, nombreRol: r.nombreRol, activo: r.activo };
    this.selectedRole = r;
    this.rolePanel = 'edit';
  }

  openPermisos(r: AdminRoleDto): void {
    this.selectedRole = r;
    this.selectedPerms = new Set(r.permissionCodes ?? []);
    this.permFilter = '';
    this.rolePanel = 'permisos';
  }

  openCopiar(r: AdminRoleDto): void {
    this.selectedRole = r;
    const others = this.rolesActivosExcept(r.id);
    this.copyFromRoleId = others[0]?.id ?? null;
    this.rolePanel = 'copiar';
  }

  saveRole(): void {
    this.ok.set(null);
    if (this.rolePanel === 'create') {
      const codigoRol = this.roleForm.codigoRol.trim();
      if (!codigoRol) {
        this.error.set('CodigoRol requerido');
        return;
      }
      this.api.createRole({ codigoRol, nombreRol: this.roleForm.nombreRol.trim() || codigoRol }).subscribe({
        next: () => {
          this.ok.set('Rol creado');
          this.rolePanel = 'list';
          this.reload();
        },
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
      return;
    }
    this.api
      .updateRole(this.roleForm.id, {
        nombreRol: this.roleForm.nombreRol.trim(),
        activo: this.roleForm.activo,
      })
      .subscribe({
        next: () => {
          this.ok.set('Rol actualizado');
          this.rolePanel = 'list';
          this.reload();
        },
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
  }

  deleteRole(r: AdminRoleDto): void {
    if (!confirm(`¿Desactivar rol ${r.codigoRol}?`)) return;
    this.api.deleteRole(r.id).subscribe({
      next: () => {
        this.ok.set('Rol desactivado');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  savePermisos(): void {
    if (!this.selectedRole) return;
    this.api.saveRolePermissions(this.selectedRole.id, [...this.selectedPerms]).subscribe({
      next: (detail) => {
        this.ok.set(`Árbol guardado (${detail.permissionCodes.length} operaciones)`);
        this.reload();
        this.rolePanel = 'list';
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  copyPermisos(): void {
    if (!this.selectedRole || !this.copyFromRoleId) return;
    this.api.copyRolePermissions(this.selectedRole.id, this.copyFromRoleId).subscribe({
      next: (detail) => {
        this.ok.set(`Permisos copiados (${detail.permissionCodes.length})`);
        this.rolePanel = 'list';
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  openCreateUser(): void {
    this.userForm = { id: 0, usuario: '', password: '', nombre: '', email: '', activo: true };
    this.userPanel = 'create';
  }

  openEditUser(u: AdminUserDto): void {
    this.userForm = {
      id: u.id,
      usuario: u.usuario,
      password: '',
      nombre: u.nombre || '',
      email: u.email || '',
      activo: u.activo,
    };
    this.selectedUser = u;
    this.userPanel = 'edit';
  }

  openUserRoles(u: AdminUserDto): void {
    this.selectedUser = u;
    const first = this.rolesActivos()[0];
    this.assignRoleId = first?.id ?? null;
    this.userPanel = 'roles';
  }

  openUserOps(u: AdminUserDto): void {
    this.selectedUser = u;
    this.userOps = null;
    this.userOpsTree = [];
    this.userOpsSelectedKeys = [];
    this.busyOps = true;
    this.userPanel = 'operaciones';
    this.error.set(null);
    this.api.getUserPermissionTree(u.id).subscribe({
      next: (dto) => {
        this.userOps = dto;
        this.userOpsTree = dto.tree as PermissionTreeNode[];
        this.userOpsSelectedKeys = this.collectSelectedKeys(this.userOpsTree);
        this.busyOps = false;
      },
      error: (e) => {
        this.busyOps = false;
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  onUserOpsSelection(keys: string[]): void {
    this.userOpsSelectedKeys = keys;
  }

  saveUserOps(): void {
    if (!this.selectedUser) return;
    this.busyOps = true;
    this.ok.set(null);
    const ids = this.userOpsSelectedKeys
      .map((k) => Number(k))
      .filter((n) => Number.isFinite(n) && n > 0);
    this.api.saveUserPermissions(this.selectedUser.id, ids).subscribe({
      next: (dto) => {
        this.userOps = dto;
        this.userOpsTree = dto.tree as PermissionTreeNode[];
        this.userOpsSelectedKeys = this.collectSelectedKeys(this.userOpsTree);
        this.busyOps = false;
        this.ok.set('Operaciones guardadas (grants directos)');
      },
      error: (e) => {
        this.busyOps = false;
        this.error.set(e?.error?.error || e.message);
      },
    });
  }

  private collectSelectedKeys(nodes: PermissionTreeNode[]): string[] {
    const out: string[] = [];
    const walk = (list: PermissionTreeNode[]) => {
      for (const n of list) {
        if (n.selected && !n.unselectable) out.push(n.key);
        if (n.children?.length) walk(n.children);
      }
    };
    walk(nodes);
    return out;
  }

  saveUser(): void {
    this.ok.set(null);
    if (this.userPanel === 'create') {
      if (!this.userForm.usuario.trim() || !this.userForm.password) {
        this.error.set('Usuario y password requeridos');
        return;
      }
      this.api
        .createUser({
          usuario: this.userForm.usuario.trim(),
          password: this.userForm.password,
          nombre: this.userForm.nombre || null,
          email: this.userForm.email || null,
        })
        .subscribe({
          next: () => {
            this.ok.set('Usuario creado');
            this.userPanel = 'list';
            this.reload();
          },
          error: (e) => this.error.set(e?.error?.error || e.message),
        });
      return;
    }
    this.api
      .updateUser(this.userForm.id, {
        nombre: this.userForm.nombre || null,
        email: this.userForm.email || null,
        password: this.userForm.password || null,
        activo: this.userForm.activo,
      })
      .subscribe({
        next: () => {
          this.ok.set('Usuario actualizado');
          this.userPanel = 'list';
          this.reload();
        },
        error: (e) => this.error.set(e?.error?.error || e.message),
      });
  }

  deleteUser(u: AdminUserDto): void {
    if (!confirm(`¿Desactivar usuario ${u.usuario}?`)) return;
    this.api.deleteUser(u.id).subscribe({
      next: () => {
        this.ok.set('Usuario desactivado');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  assignRole(): void {
    if (!this.selectedUser || !this.assignRoleId) return;
    this.api.assignRoleToUser(this.selectedUser.id, this.assignRoleId).subscribe({
      next: () => {
        this.ok.set('Rol asignado');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }

  unassignRole(idRol: number): void {
    if (!this.selectedUser) return;
    this.api.unassignRoleFromUser(this.selectedUser.id, idRol).subscribe({
      next: () => {
        this.ok.set('Rol quitado');
        this.reload();
      },
      error: (e) => this.error.set(e?.error?.error || e.message),
    });
  }
}
