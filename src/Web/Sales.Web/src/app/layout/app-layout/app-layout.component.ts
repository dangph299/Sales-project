import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { NzBreadCrumbModule } from 'ng-zorro-antd/breadcrumb';
import { NzLayoutModule } from 'ng-zorro-antd/layout';
import { SignalrConnectionService } from '../../core/realtime/signalr-connection.service';
import { AppHeaderComponent } from '../app-header/app-header.component';
import { AppSidebarComponent } from '../app-sidebar/app-sidebar.component';
import { AppStatusBarComponent } from '../app-status-bar/app-status-bar.component';
import { BreadcrumbService } from '../breadcrumb/breadcrumb.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    RouterOutlet,
    NzBreadCrumbModule,
    NzLayoutModule,
    AppHeaderComponent,
    AppSidebarComponent,
    AppStatusBarComponent
  ],
  templateUrl: './app-layout.component.html',
  styleUrl: './app-layout.component.scss'
})
export class AppLayoutComponent {
  private readonly breadcrumbService = inject(BreadcrumbService);
  private readonly realtimeConnection = inject(SignalrConnectionService);

  readonly breadcrumbs = this.breadcrumbService.breadcrumbs;
  readonly pageTitle = this.breadcrumbService.pageTitle;

  readonly sidebarCollapsed = signal(false);
  readonly errorMessage = signal('');

  onSignedOut(): void {
    // Layout owns the shell, not any feature: it closes the transport, and
    // feature services re-subscribe on the next connect.
    void this.realtimeConnection.stop();
  }
}
