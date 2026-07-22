import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { NzCardModule } from 'ng-zorro-antd/card';

@Component({
  selector: 'app-dashboard-section',
  standalone: true,
  imports: [CommonModule, NzCardModule],
  templateUrl: './dashboard-section.component.html',
  styleUrl: './dashboard-section.component.scss'
})
export class DashboardSectionComponent {
  @Input({ required: true }) title = '';
  @Input() description = '';
}
