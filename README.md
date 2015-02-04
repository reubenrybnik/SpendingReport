# SpendingReport
A simple web application for getting categorized summary reports on personal spending trends.

The goal of this project is to design a fairly simple website for recording categorized transactions and creating summary reports on the distribution of spending in each category throughout a selected time period. Categories can have parents, so the reports will start at the top level of categories and provide options for viewing each level and for drilling down into subcategories for individual categories.

At this time, I am working on the database, data context, and data models for the site. I decided to remove EntityFramework from the project for a few reasons. First, I don't often get a chance to put together a database from scratch and I like doing so. Second, I found a few interesting ways to interact with databases (most notably SqlAdapter.FillSchema) near the end of my time at Microsoft and wanted to see what I could do with them in my C# code.