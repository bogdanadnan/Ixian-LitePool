import { Component, OnInit } from '@angular/core';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

declare interface BlockData {
    BlockNum: number;
    TimeStamp: string;
    Reward: number;
    Status: string;
    Miner: string;
}

@Component({
    selector: 'blocks-cmp',
    moduleId: module.id,
    templateUrl: 'blocks.component.html'
})

export class BlocksComponent implements OnInit {
    public blocksData: BlockData[];

    constructor(private http: HttpClient) { }

    ngOnInit() {
        this.updateBlocksData();
    }

    public updateBlocksData() {
        this.http.get("/api/blocks").subscribe((data: BlockData[]) => {
            this.blocksData = data;
        });
    }
}
