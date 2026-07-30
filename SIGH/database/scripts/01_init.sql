-- Script de inicialização do banco de dados SQL Server para o SIGH
USE master;
GO

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'SIGH_DB')
BEGIN
    CREATE DATABASE SIGH_DB;
END
GO

USE SIGH_DB;
GO

-- Schema e tabelas base serão aplicadas via Entity Framework Core Migrations na próxima sprint.
