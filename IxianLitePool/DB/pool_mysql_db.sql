CREATE TABLE IF NOT EXISTS Block (
blockNum integer primary key not null ,
difficulty bigint ,
`version` integer ,
checksum blob ,
`timeStamp` datetime );

CREATE TABLE IF NOT EXISTS Miner (
id integer primary key auto_increment not null ,
address varchar(100) ,
lastSeen datetime ,
pending double default 0.00);

CREATE TABLE IF NOT EXISTS Notification (
id integer primary key auto_increment not null ,
`type` integer ,
notification varchar(250) ,
active integer );

CREATE TABLE IF NOT EXISTS Payment (
id integer primary key auto_increment not null ,
minerId integer ,
`timeStamp` datetime ,
value double ,
fee double ,
txId varchar(100) ,
verified integer default 0,
paymentSession varchar(100) );

CREATE TABLE IF NOT EXISTS PoolBlock (
blockNum integer primary key not null ,
miningStart datetime ,
miningEnd datetime ,
resolution integer ,
poolDifficulty bigint );

CREATE TABLE IF NOT EXISTS PoolState (
`key` varchar(50) primary key not null,
value varchar(100) );

CREATE TABLE IF NOT EXISTS PowData (
id integer primary key auto_increment not null ,
blockNum integer ,
solvedBlock integer ,
solverAddress varchar(100) ,
txId varchar(100) ,
reward double );

CREATE TABLE IF NOT EXISTS Share (
id integer primary key auto_increment not null ,
minerId integer ,
workerId integer ,
`timeStamp` datetime ,
blockNum integer ,
difficulty bigint ,
nonce varchar(150) ,
blockResolved integer ,
processed integer default 0,
paymentSession varchar(100) );

CREATE TABLE IF NOT EXISTS Worker (
id integer primary key auto_increment not null ,
minerId integer ,
name varchar(50) ,
miningApp varchar(25) ,
hashrate float ,
lastSeen datetime );

CREATE UNIQUE INDEX IF NOT EXISTS Miner_address on Miner(address);
CREATE INDEX IF NOT EXISTS Miner_pending on Miner(pending);
CREATE INDEX IF NOT EXISTS Notification_active on Notification(active);
CREATE INDEX IF NOT EXISTS Payment_minerId on Payment(minerId);
CREATE INDEX IF NOT EXISTS PoolState_key on PoolState(`key`);
CREATE INDEX IF NOT EXISTS PowData_blockNum on PowData(blockNum);
CREATE INDEX IF NOT EXISTS PowData_solvedBlock on PowData(solvedBlock);
CREATE INDEX IF NOT EXISTS PowData_solverAddress on PowData(solverAddress);
CREATE INDEX IF NOT EXISTS Share_blockNum on Share(blockNum);
CREATE INDEX IF NOT EXISTS Share_blockResolved on Share(blockResolved);
CREATE INDEX IF NOT EXISTS Share_minerId on Share(minerId);
CREATE INDEX IF NOT EXISTS Share_nonce on Share(nonce);
CREATE INDEX IF NOT EXISTS Share_paymentSession on Share(paymentSession);
CREATE INDEX IF NOT EXISTS Share_processed on Share(processed);
CREATE INDEX IF NOT EXISTS Share_workerId on Share(workerId);
CREATE INDEX IF NOT EXISTS Share_timeStamp on Share(`timeStamp`);
CREATE INDEX IF NOT EXISTS Worker_minerId on Worker(minerId);

