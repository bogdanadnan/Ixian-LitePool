import { Component, OnInit } from '@angular/core';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

declare interface PaymentData {
    Miner: string;
    TimeStamp: string;
    Value: number;
    TxId: string;
    Status: string;
}

@Component({
    selector: 'payments-cmp',
    moduleId: module.id,
    templateUrl: 'payments.component.html'
})

export class PaymentsComponent implements OnInit {
    public paymentsData: PaymentData[];

    constructor(private http: HttpClient) { }

    ngOnInit() {
        this.updatePaymentsData();
    }

    public updatePaymentsData() {
        this.http.get("/api/payments").subscribe((data: PaymentData[]) => {
            this.paymentsData = data;
            setTimeout(() => {
                $('#paymentsTable').DataTable().destroy();
                $('#paymentsTable').DataTable({
                    pagingType: 'full_numbers',
                    pageLength: 10,
                    processing: true,
                    lengthMenu: [5, 10, 25]
                });
            }, 0);
        });
    }
}
