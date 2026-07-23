import { ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { en_US, provideNzI18n } from 'ng-zorro-antd/i18n';
import { provideNzIcons } from 'ng-zorro-antd/icon';
import {
  ApiOutline,
  AppstoreOutline,
  CheckCircleOutline,
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
  UserOutline
} from '@ant-design/icons-angular/icons';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideAnimations(),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideNzI18n(en_US),
    provideRouter(routes),
    provideNzIcons([
      ApiOutline,
      AppstoreOutline,
      CheckCircleOutline,
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
      UserOutline
    ]),
    importProvidersFrom(FormsModule, ReactiveFormsModule)
  ]
};
