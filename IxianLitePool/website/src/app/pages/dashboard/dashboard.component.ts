import { Component, OnInit } from '@angular/core';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { interval } from 'rxjs';
import Chart from 'chart.js';
declare var Config;

@Component({
    selector: 'dashboard-cmp',
    moduleId: module.id,
    templateUrl: 'dashboard.component.html'
})

export class DashboardComponent implements OnInit{

  public canvas: any;
  public ctx;
  public chartColor;
  public chartEmail;
    public chartHours;

    private oneMinuteUpdater: any;
    public notifications: NotificationData[];

    constructor(private http: HttpClient) { }

    ngOnInit() {
        document.getElementById("poolUrl").innerText = Config.poolUrl;
        document.getElementById("poolFee").innerText = (Config.poolFee * 100).toFixed(2);
        document.getElementById("blockReward").innerText = Config.blockReward.toFixed(2);

        this.oneMinuteUpdater = interval(60000).subscribe(i => {
            this.updateDashboardData();
        });

        this.updateDashboardData();

      this.chartColor = "#FFFFFF";

      var speedCanvas = document.getElementById("poolHashrateChart");

      var dataFirst = {
        data: [0, 19, 15, 20, 30, 40, 40, 50, 25, 30, 50, 70],
        fill: false,
        borderColor: '#fbc658',
        backgroundColor: 'transparent',
        pointBorderColor: '#fbc658',
        pointRadius: 4,
        pointHoverRadius: 4,
        pointBorderWidth: 8,
      };

      var dataSecond = {
        data: [0, 5, 10, 12, 20, 27, 30, 34, 42, 45, 55, 63],
        fill: false,
        borderColor: '#51CACF',
        backgroundColor: 'transparent',
        pointBorderColor: '#51CACF',
        pointRadius: 4,
        pointHoverRadius: 4,
        pointBorderWidth: 8
      };

      var speedData = {
        labels: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"],
        datasets: [dataFirst, dataSecond]
      };

      var chartOptions = {
        legend: {
          display: false,
          position: 'top'
        }
      };

      var lineChart = new Chart(speedCanvas, {
        type: 'line',
        hover: false,
        data: speedData,
        options: chartOptions
      });
    }

    public ngOnDestroy() {
        this.oneMinuteUpdater.unsubscribe();
    }

    public updateDashboardData() {
        this.http.get("/api/dashboard").subscribe((data: DashboardStatus) => {
            document.getElementById("networkHeight").innerText = data.NetworkBlockHeight.toString();
            document.getElementById("miningBlock").innerText = data.ActiveMiningBlock.toString();
            document.getElementById("minersCount").innerText = data.Miners.toString();
            document.getElementById("workersCount").innerText = data.Workers.toString();
            document.getElementById("totalPayments").innerText = data.TotalPayments.toFixed(2);
            document.getElementById("totalPending").innerText = data.TotalPending.toFixed(2);
            document.getElementById("poolHashrate").innerText = data.PoolHashrate.toFixed(2);
            document.getElementById("poolDifficulty").innerText = (Math.floor(data.PoolDifficulty / 10000000000000)).toString();
            document.getElementById("blocksMined").innerText = data.BlocksMined.toString();
            document.getElementById("ixiPrice").innerText = data.IxiPrice.toFixed(3);
            this.notifications = data.Notifications;
        });
    }
}

interface NotificationData {
    Type: string;
    Notification: string;
};

interface DashboardStatus {
    NetworkBlockHeight: number;
    ActiveMiningBlock: number;
    Miners: number;
    Workers: number;
    TotalPayments: number;
    TotalPending: number;
    PoolHashrate: number;
    PoolDifficulty: number;
    BlocksMined: number;
    IxiPrice: number;
    Notifications: NotificationData[];
};
