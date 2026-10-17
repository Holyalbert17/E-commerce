import { NgTemplateOutlet } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';

/** Nodo FancyTree ERP (TreeEmpresaUsuarioOperacion). */
export interface PermissionTreeNode {
  key: string;
  codigoPermission: string;
  title: string;
  folder: boolean;
  selected: boolean;
  unselectable: boolean;
  source: 'P' | 'R' | null;
  children: PermissionTreeNode[];
}

@Component({
  standalone: true,
  selector: 'app-permission-tree',
  imports: [FormsModule, NgTemplateOutlet],
  template: `
    <div class="ptree">
      <div class="ptree-toolbar">
        <input
          class="form-control form-control-sm"
          placeholder="Filtrar operaciones…"
          [(ngModel)]="filter"
          (ngModelChange)="applyFilter()"
        />
        <div class="btn-group btn-group-sm">
          <button type="button" class="btn btn-default" (click)="expandAll(true)" title="Expandir">
            <i class="fas fa-plus-square"></i>
          </button>
          <button type="button" class="btn btn-default" (click)="expandAll(false)" title="Colapsar">
            <i class="fas fa-minus-square"></i>
          </button>
          <button type="button" class="btn btn-default" (click)="selectAll(true)">Todo</button>
          <button type="button" class="btn btn-default" (click)="selectAll(false)">Nada</button>
        </div>
      </div>
      <p class="ptree-legend text-muted small mb-2">
        <span class="src-r">Verde / candado</span> = permiso por rol · Checkbox = grant directo
      </p>
      <div class="ptree-body">
        @for (n of visible; track n.key) {
          <ng-container *ngTemplateOutlet="nodeTpl; context: { $implicit: n, depth: 0 }" />
        } @empty {
          <div class="dx-empty py-4"><p>Sin operaciones.</p></div>
        }
      </div>
    </div>

    <ng-template #nodeTpl let-n let-depth="depth">
      @if (isVisible(n)) {
        <div class="ptree-row" [style.paddingLeft.px]="12 + depth * 18">
          @if (n.folder) {
            <button type="button" class="ptree-twist" (click)="toggle(n)">
              <i
                class="fas"
                [class.fa-caret-down]="expanded.has(n.key)"
                [class.fa-caret-right]="!expanded.has(n.key)"
              ></i>
            </button>
          } @else {
            <span class="ptree-twist-spacer"></span>
          }
          <label class="ptree-label" [class.from-role]="n.unselectable" [class.from-direct]="n.source === 'P'">
            <input
              type="checkbox"
              [checked]="n.selected"
              [disabled]="n.unselectable || readonly"
              (change)="onToggle(n, $event)"
            />
            <span class="ptree-title" [innerHTML]="titleHtml(n)"></span>
            <code class="ptree-code">{{ n.codigoPermission }}</code>
          </label>
        </div>
        @if (n.folder && expanded.has(n.key)) {
          @for (c of n.children; track c.key) {
            <ng-container *ngTemplateOutlet="nodeTpl; context: { $implicit: c, depth: depth + 1 }" />
          }
        }
      }
    </ng-template>
  `,
  styles: [
    `
      .ptree {
        border: 1px solid #dee2e6;
        border-radius: 8px;
        background: #fff;
      }
      .ptree-toolbar {
        display: flex;
        gap: 0.5rem;
        padding: 0.65rem 0.75rem;
        border-bottom: 1px solid #e9ecef;
        background: #f8f9fa;
        align-items: center;
      }
      .ptree-toolbar input {
        flex: 1;
        max-width: 280px;
      }
      .ptree-legend {
        padding: 0 0.75rem;
        margin-top: 0.5rem;
      }
      .ptree-legend .src-r {
        color: #1e7e34;
        font-weight: 600;
      }
      .ptree-body {
        max-height: 480px;
        overflow: auto;
        padding: 0.35rem 0 0.75rem;
      }
      .ptree-row {
        display: flex;
        align-items: flex-start;
        gap: 0.25rem;
        min-height: 1.75rem;
      }
      .ptree-twist,
      .ptree-twist-spacer {
        width: 1.25rem;
        flex: 0 0 1.25rem;
        border: 0;
        background: transparent;
        color: #6c757d;
        padding: 0.15rem 0;
        line-height: 1;
      }
      .ptree-label {
        display: flex;
        flex-wrap: wrap;
        align-items: baseline;
        gap: 0.35rem 0.6rem;
        margin: 0;
        font-weight: 400;
        cursor: pointer;
      }
      .ptree-label.from-role .ptree-title {
        color: #1e7e34;
        font-weight: 600;
      }
      .ptree-label.from-direct .ptree-title {
        color: #0d6efd;
      }
      .ptree-code {
        font-size: 0.7rem;
        color: #868e96;
      }
      .ptree-title {
        font-size: 0.9rem;
      }
    `,
  ],
})
export class PermissionTreeComponent implements OnChanges {
  @Input() nodes: PermissionTreeNode[] = [];
  @Input() readonly = false;
  @Output() readonly selectionChange = new EventEmitter<string[]>();

  filter = '';
  visible: PermissionTreeNode[] = [];
  expanded = new Set<string>();

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['nodes']) {
      this.visible = this.nodes ?? [];
      this.expandAll(true);
    }
  }

  applyFilter(): void {
    this.visible = [...(this.nodes ?? [])];
  }

  isVisible(n: PermissionTreeNode): boolean {
    const q = this.filter.trim().toLowerCase();
    if (!q) return true;
    if (n.title.toLowerCase().includes(q) || n.codigoPermission.toLowerCase().includes(q)) return true;
    return (n.children ?? []).some((c) => this.isVisible(c));
  }

  titleHtml(n: PermissionTreeNode): string {
    const t = this.escape(n.title || n.codigoPermission);
    if (n.unselectable) return `<strong>${t}</strong> <i class="fas fa-lock fa-xs"></i>`;
    return t;
  }

  toggle(n: PermissionTreeNode): void {
    if (this.expanded.has(n.key)) this.expanded.delete(n.key);
    else this.expanded.add(n.key);
  }

  expandAll(open: boolean): void {
    this.expanded.clear();
    if (!open) return;
    const walk = (list: PermissionTreeNode[]) => {
      for (const n of list) {
        if (n.folder) {
          this.expanded.add(n.key);
          walk(n.children ?? []);
        }
      }
    };
    walk(this.nodes ?? []);
  }

  selectAll(on: boolean): void {
    const walk = (list: PermissionTreeNode[]) => {
      for (const n of list) {
        if (!n.unselectable) {
          n.selected = on;
          n.source = on ? 'P' : null;
        }
        walk(n.children ?? []);
      }
    };
    walk(this.nodes ?? []);
    this.emit();
  }

  onToggle(n: PermissionTreeNode, ev: Event): void {
    if (n.unselectable || this.readonly) return;
    const checked = (ev.target as HTMLInputElement).checked;
    this.setSubtree(n, checked);
    this.emit();
  }

  private setSubtree(n: PermissionTreeNode, checked: boolean): void {
    if (!n.unselectable) {
      n.selected = checked;
      n.source = checked ? 'P' : null;
    }
    for (const c of n.children ?? []) this.setSubtree(c, checked);
  }

  private emit(): void {
    const ids: string[] = [];
    const walk = (list: PermissionTreeNode[]) => {
      for (const n of list) {
        if (n.selected && !n.unselectable) ids.push(n.key);
        walk(n.children ?? []);
      }
    };
    walk(this.nodes ?? []);
    this.selectionChange.emit(ids);
  }

  private escape(s: string): string {
    return s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
  }
}
