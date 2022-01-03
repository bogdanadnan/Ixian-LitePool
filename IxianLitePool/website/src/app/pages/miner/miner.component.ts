import { Component, OnInit } from '@angular/core';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ActivatedRoute } from '@angular/router'
import { interval } from 'rxjs';

declare interface MinerDashboardInfo {
    Workers: Number,
    Hashrate: Number,
    Pending: Number,
    Payments: Number
};

declare interface MinerWorkerInfo {
    Name: string,
    Hashrate: Number,
    Shares: Number,
    LastSeen: string
};

declare interface MinerPaymentInfo {
    TxId: string,
    TimeStamp: string,
    Value: Number,
    Status: string
};

declare interface CheckAddressInfo {
    Valid: boolean
};

@Component({
    selector: 'miner-cmp',
    moduleId: module.id,
    templateUrl: 'miner.component.html'
})

export class MinerComponent implements OnInit {
    private address: String;
    private initialized: boolean;
    private oneMinuteUpdater: any;
    public workerData: MinerWorkerInfo[];
    public paymentData: MinerPaymentInfo[];

    constructor(private http: HttpClient, private route: ActivatedRoute) { }

    ngOnInit() {
        if (this.initialized) {
            this.oneMinuteUpdater.unsubscribe();
        }
        this.initialized = false;
        this.route.params.subscribe(param => {
            $('#validInfo').addClass('d-none');
            $('#invalidInfo').addClass('d-none');
            this.address = param.address;
            this.checkMiner();
        });
    }

    public ngOnDestroy() {
        if (this.initialized) {
            this.oneMinuteUpdater.unsubscribe();
        }
    }

    public setupMinerData() {
        this.oneMinuteUpdater = interval(60000).subscribe(i => {
            this.updateMinerData();
        });
        this.updateMinerData();
        this.updateWorkerData();
        this.updatePaymentsData();
        this.initialized = true;
    }

    public activateTab(tabId: String) {
        $(".nav-link").removeClass("active");
        $("#" + tabId + 'Link').addClass("active");
        $(".tab-pane").removeClass("active");
        $("#" + tabId + 'Tab').addClass("active");
    }

    public checkMiner() {
        this.http.get("/api/miner/" + this.address + "/verify").subscribe((data: CheckAddressInfo) => {
            if (data.Valid) {
                $('#validInfo').removeClass('d-none');
                this.setupMinerData();
            }
            else {
                $('#invalidInfo').removeClass('d-none');
            }
        });
    }

    public updateMinerData() {
        this.http.get("/api/miner/" + this.address + "/dashboard").subscribe((data: MinerDashboardInfo) => {
            document.getElementById("workersCount").innerText = data.Workers.toString();
            document.getElementById("hashrate").innerText = data.Hashrate.toFixed(2);
            document.getElementById("payments").innerText = data.Payments.toFixed(2);
            document.getElementById("pending").innerText = data.Pending.toFixed(2);
        });
    }

    public updateWorkerData() {
        this.http.get("/api/miner/" + this.address + "/workers").subscribe((data: MinerWorkerInfo[]) => {
            this.workerData = data;
            setTimeout(() => {
                $('#workersTable').DataTable().destroy();
                $('#workersTable').DataTable({
                    pagingType: 'full_numbers',
                    pageLength: 10,
                    processing: true,
                    lengthMenu: [5, 10, 25]
                });
            }, 0);
        });
    }

    public updatePaymentsData() {
        this.http.get("/api/miner/" + this.address + "/payments").subscribe((data: MinerPaymentInfo[]) => {
            this.paymentData = data;
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
