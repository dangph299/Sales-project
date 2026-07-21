import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';
import { NzTableModule } from 'ng-zorro-antd/table';
import { PageStateComponent } from '../../../../shared/components/page-state/page-state.component';
import { CommonStore } from '../../services/common-store.service';

@Component({
  selector: 'app-common-page',
  standalone: true,
  imports: [CommonModule, NzCardModule, NzTableModule, PageStateComponent],
  templateUrl: './common-page.component.html',
  styleUrl: './common-page.component.scss'
})
export class CommonPageComponent implements OnInit {
  private readonly common = inject(CommonStore);

  readonly colors = this.common.colors;
  readonly sizes = this.common.sizes;
  readonly loading = this.common.loading;
  readonly errorMessage = this.common.loadError;

  ngOnInit(): void {
    void this.common.ensureLoaded();
  }

  reload(): void {
    void this.common.reload();
  }
}
