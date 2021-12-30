import { Component, OnInit } from '@angular/core';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

declare interface MinerInfo {

}

@Component({
    selector: 'miner-cmp',
    moduleId: module.id,
    templateUrl: 'miner.component.html'
})

export class MinerComponent implements OnInit {
    constructor(private http: HttpClient) { }

    ngOnInit() {
        this.updateMinerData();
    }

    public updateMinerData() {
        this.http.get("/api/miner").subscribe((data: MinerInfo) => {
        });
    }
}
