import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { AdminLayoutRoutes } from './admin-layout.routing';

import { DashboardComponent }       from '../../pages/dashboard/dashboard.component';
import { MinersComponent } from '../../pages/miners/miners.component';
import { BlocksComponent } from '../../pages/blocks/blocks.component';
import { PaymentsComponent } from '../../pages/payments/payments.component';
import { MinerComponent } from '../../pages/miner/miner.component';

import { NgbModule } from '@ng-bootstrap/ng-bootstrap';

@NgModule({
  imports: [
    CommonModule,
    RouterModule.forChild(AdminLayoutRoutes),
    FormsModule,
    NgbModule
  ],
  declarations: [
      DashboardComponent,
      MinersComponent,
      BlocksComponent,
      PaymentsComponent,
      MinerComponent
  ]
})

export class AdminLayoutModule {}
