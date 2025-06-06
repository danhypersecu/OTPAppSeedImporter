-- Create test database with required schema
-- Run this with: sqlite3 test_database.db < create_test_db.sql

-- Create ft_tokenspec table
CREATE TABLE ft_tokenspec (
    specid INTEGER PRIMARY KEY AUTOINCREMENT, 
    name TEXT NOT NULL,                       
    sn_length INTEGER NOT NULL,              
    token_interval INTEGER NOT NULL,         
    checksum TEXT NOT NULL,      
    algorithm TEXT NOT NULL                
);

-- Create ft_tokeninfo table
CREATE TABLE "ft_tokeninfo" (
    "tknid" INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    "token" varchar(32) NOT NULL,
    "pubkey" varchar(1024) NOT NULL DEFAULT '',
    "authnum" varchar(32) DEFAULT '0',
    "physicaltype" INTEGER DEFAULT 0,
    "producttype" INTEGER DEFAULT 0,
    "specid" INTEGER NOT NULL,
    "importtime" INTEGER DEFAULT 0,
    "pubkeystate" INTEGER DEFAULT 0,
    "pubkeyfactor" varchar(128),
    "tknifmid" INTEGER NOT NULL,
    "tknofmid" INTEGER NOT NULL,
    "crust" varchar(32),
    "tknexttype" varchar(128),
    "tkntype" INTEGER NOT NULL,
    "tknstate" INTEGER,
    "tksendnumber" varchar(64),
    "tknvaliddate" INTEGER,
    CONSTRAINT "fk_tkninfo_tknspec_specid" FOREIGN KEY ("specid") REFERENCES "ft_tokenspec" ("specid") ON DELETE CASCADE ON UPDATE CASCADE,
    UNIQUE ("token" ASC)
);

-- Insert a sample token specification
INSERT INTO ft_tokenspec (name, sn_length, token_interval, checksum, algorithm)
VALUES ('Sample TOTP Spec', 12, 30, 'sha256', 'totp'); 