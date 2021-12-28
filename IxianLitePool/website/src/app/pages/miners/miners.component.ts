import { Component, OnInit } from '@angular/core';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

declare interface MinerData {
    Address: string;
    LastSeen: string;
    RoundShares: number;
    Pending: number;
    HashRate: number;
}

@Component({
    selector: 'miners-cmp',
    moduleId: module.id,
    templateUrl: 'miners.component.html'
})

export class MinersComponent implements OnInit{
    public minersData: MinerData[];

    constructor(private http: HttpClient) { }

    ngOnInit() {
        this.updateMinersData();
    }

    public updateMinersData() {
        this.http.get("/api/miners").subscribe((data: MinerData[]) => {
            this.minersData = data;
        });
    }
}
    