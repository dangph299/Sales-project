import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { NzAlertModule } from 'ng-zorro-antd/alert';

@Component({
  selector: 'app-status-bar',
  standalone: true,
  imports: [CommonModule, NzAlertModule],
  templateUrl: './app-status-bar.component.html',
  styleUrl: './app-status-bar.component.scss'
})
export class AppStatusBarComponent {
  @Input() errorMessage = '';
}
