import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { en_US, provideNzI18n } from 'ng-zorro-antd/i18n';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import {
  ApiOutline,
  AppstoreOutline,
  CheckCircleOutline,
  CloseCircleOutline,
  DatabaseOutline,
  DashboardOutline,
  DollarOutline,
  DownOutline,
  FolderOpenOutline,
  InboxOutline,
  LogoutOutline,
  MenuFoldOutline,
  MenuUnfoldOutline,
  PlusOutline,
  ReloadOutline,
  RightOutline,
  SearchOutline,
  ShoppingOutline,
  ShoppingCartOutline,
  TagsOutline,
  TeamOutline,
  UserAddOutline,
  UserOutline,
  WarningOutline
} from '@ant-design/icons-angular/icons';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideAnimations(),
    provideHttpClient(),
    provideNzI18n(en_US),
    provideRouter(routes),
    provideNzIcons([
      ApiOutline,
      AppstoreOutline,
      CheckCircleOutline,
      CloseCircleOutline,
      DatabaseOutline,
      DashboardOutline,
      DollarOutline,
      DownOutline,
      FolderOpenOutline,
      InboxOutline,
      LogoutOutline,
      MenuFoldOutline,
      MenuUnfoldOutline,
      PlusOutline,
      ReloadOutline,
      RightOutline,
      SearchOutline,
      ShoppingOutline,
      ShoppingCartOutline,
      TagsOutline,
      TeamOutline,
      UserAddOutline,
      UserOutline,
      WarningOutline
    ]),
    importProvidersFrom(FormsModule, ReactiveFormsModule)
  ]
};
