import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { seededColors, seededSizes } from '../reference-data/reference-data';

@Component({
  selector: 'app-reference-data-page',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="page-header">
      <div>
        <p class="eyebrow">Catalog</p>
        <h1>Colors and Sizes</h1>
        <p>Reference data read-only. UI hien seed deterministic hien co trong source control.</p>
      </div>
    </section>

    <section class="notice-panel">
      <strong>Backend gap</strong>
      <p>Sales API chua expose endpoint doc Color/Size. Man hinh nay dung dung seed ID/code tu migration de test ProductVariant flow.</p>
    </section>

    <section class="content-grid two">
      <article class="panel-card">
        <div class="section-title">
          <h2>Color List</h2>
          <span>{{ colors.length }} colors</span>
        </div>
        <table class="data-table">
          <thead>
            <tr>
              <th>Preview</th>
              <th>Code</th>
              <th>Name</th>
              <th>Hex</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let color of colors">
              <td><span class="color-swatch" [style.background]="color.hexCode || '#ffffff'"></span></td>
              <td>{{ color.code }}</td>
              <td>{{ color.name }}</td>
              <td>{{ color.hexCode || '-' }}</td>
            </tr>
          </tbody>
        </table>
      </article>

      <article class="panel-card">
        <div class="section-title">
          <h2>Size List</h2>
          <span>Sorted by SortOrder</span>
        </div>
        <table class="data-table">
          <thead>
            <tr>
              <th>Code</th>
              <th>Name</th>
              <th>SortOrder</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let size of sizes">
              <td>{{ size.code }}</td>
              <td>{{ size.name }}</td>
              <td>{{ size.sortOrder }}</td>
            </tr>
          </tbody>
        </table>
      </article>
    </section>
  `
})
export class ReferenceDataPageComponent {
  readonly colors = seededColors;
  readonly sizes = seededSizes;
}
