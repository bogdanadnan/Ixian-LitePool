import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { ToastrModule } from "ngx-toastr";

import { HttpClientModule } from '@angular/common/http';
import { DataTablesModule } from 'angular-datatables';

import { SidebarModule } from './sidebar/sidebar.module';
import { FooterModule } from './shared/footer/footer.module';
import { NavbarModule} from './shared/navbar/navbar.module';

import { AppComponent } from './app.component';
import { AppRoutes } from './app.routing';

import { AdminLayoutComponent } from './layouts/admin-layout/admin-layout.component';


@NgModule({
  declarations: [
    AppComponent,
    AdminLayoutComponent
  ],
  imports: [
      BrowserAnimationsModule,
      HttpClientModule,
      DataTablesModule,
    RouterModule.forRoot(AppRoutes,{
    useHash: true,
    relativeLinkResolution: 'legacy'
}),
    SidebarModule,
    NavbarModule,
    ToastrModule.forRoot(),
    FooterModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
