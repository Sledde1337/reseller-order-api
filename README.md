# Reseller Order API

A REST API for managing products, resellers, orders and stock levels
for a maker-tools distributor (3D printers, filament, tools).

## Status
Work in progress 

## Planned features
- Product and reseller management
- Order lifecycle (pending → confirmed → shipped → delivered)
- Stock tracking with concurrency-safe updates
- JWT authentication with role-based access

## Tech stack
ASP.NET Core · EF Core · PostgreSQL · Docker · xUnit