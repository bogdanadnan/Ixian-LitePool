import { Routes } from '@angular/router';

import { DashboardComponent } from '../../pages/dashboard/dashboard.component';
import { MinersComponent } from '../../pages/miners/miners.component';
import { BlocksComponent } from '../../pages/blocks/blocks.component';
import { PaymentsComponent } from '../../pages/payments/payments.component';

export const AdminLayoutRoutes: Routes = [
    { path: 'dashboard',      component: DashboardComponent },
    { path: 'miners', component: MinersComponent },
    { path: 'blocks', component: BlocksComponent },
    { path: 'payments', component: PaymentsComponent }
];
