import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { en_US, provideNzI18n } from 'ng-zorro-antd/i18n';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import {
  AppstoreOutline,
  DatabaseOutline,
  DashboardOutline,
  DownOutline,
  FolderOpenOutline,
  MenuFoldOutline,
  MenuUnfoldOutline,
  ReloadOutline,
  RightOutline,
  SearchOutline,
  ShoppingCartOutline,
  TagsOutline,
  UserOutline
} from '@ant-design/icons-angular/icons';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideAnimations(),
    provideHttpClient(),
    provideNzI18n(en_US),
    provideRouter(routes),
    provideNzIcons([
      AppstoreOutline,
      DatabaseOutline,
      DashboardOutline,
      DownOutline,
      FolderOpenOutline,
      MenuFoldOutline,
      MenuUnfoldOutline,
      ReloadOutline,
      RightOutline,
      SearchOutline,
      ShoppingCartOutline,
      TagsOutline,
      UserOutline
    ]),
    importProvidersFrom(FormsModule, ReactiveFormsModule)
  ]
};
