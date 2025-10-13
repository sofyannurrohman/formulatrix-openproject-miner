# OpenProject Data Mining

📈 **OpenProject Data Mining** is a .NET Core MVC application designed to extract, process, and visualize productivity metrics from Formulatrix OpenProject. The tool helps quantify team member productivity by analyzing task completion times, status transitions, and work item volumes.

---

## Table of Contents

- [Features](#features)  
- [Goals](#goals)  
- [MVP](#mvp)  
- [Process Flow](#process-flow)  
- [Dashboard UI](#dashboard-ui)  
- [Productivity Metrics](#productivity-metrics)  
- [API Data Sources](#api-data-sources)  
- [Setup](#setup)  
- [Future Improvements](#future-improvements)

---

## Features

- Fetch projects and team members from OpenProject API  
- Extract work packages (user stories & issues) with activity logs  
- Calculate productivity metrics per team member  
- Display interactive dashboards with tables and charts  
- Drill-down views for individual member task details  

---

## Goals

1. Measure team member productivity based on work packages.  
2. Retrieve projects, members, and specify goal periods.  
3. Extract the number of user stories and issues worked on by each member.  
4. Analyze activity logs to assess productivity (status change durations, rework loops).  
5. Compare productivity across team members.  

---

## MVP (Minimum Viable Product)

- Calculate time from "In Progress" → "Done" for work packages  
- Aggregate metrics per team member within a specified goal period  
- Display results in a sortable, filterable table  
- Show charts (bar, line, pie) for quick insights  

---

## Process Flow

1. **Fetch Projects** → GET `/api/v3/projects`  
2. **Fetch Team Members** → GET `/api/v3/memberships`  
3. **Fetch Work Packages** → GET `/api/v3/work_packages` (filtered by project, user, type, date range)  
4. **Fetch Work Package Activities** → GET `/api/v3/work_packages/{id}/activities`  
5. **Calculate Metrics** per member:  
   - Avg. duration from "In Progress" → "Done"  
   - Total tasks completed  
   - Productivity score (`TotalTasks / AvgDuration`)  
6. **Display Results** → tables and charts in dashboard  

---

## Dashboard UI

### Project & Goal Period Selection
- Dropdowns for selecting project and goal period (e.g., 2025-H1, 2025-H2)  

### Team Productivity Summary
- Table with sortable columns:  
  - Member Name  
  - Total Tasks (User Story / Issue)  
  - Avg. Duration (Days)  
  - Total Completed  
  - Productivity Score  
- Charts:  
  - Horizontal bar chart for average duration  
  - Pie chart for task distribution  
  - Line/dot charts for trends  

### Member Details (Drill-Down)
- Paginated table with task-level details:  
  - ID, Type, Subject  
  - Start / End timestamps  
  - Duration, Status History  
  - Notes (e.g., rework loops)  

---

## Productivity Metrics

- **Task Difficulty**:  
  - Easy → ≤3 activities (linear progression)  
  - Difficult → >3 activities (loops / rework)  
- **Completion Time**: `(Done - In Progress)` in days  
- **Data Sources**: Activity logs and time entries from OpenProject API  

---

## API Data Sources

| Resource | Endpoint | Notes |
|----------|----------|------|
| Projects | `/api/v3/projects` | List all projects |
| Memberships | `/api/v3/memberships` | Filter by project or user |
| Types | `/api/v3/types` | Identify User Story / Issue |
| Statuses | `/api/v3/statuses` | Identify "In Progress", "Developed", "Done" |
| Work Packages | `/api/v3/work_packages` | Filter by project, assignee, type, date range |
| Activities | `/api/v3/work_packages/{id}/activities` | Status change logs |
| Time Entries | `/api/v3/time_entries` | Sum of time spent per work package |

> All endpoints require API key authentication.

---

## Setup

1. Clone the repository:
```bash
git clone https://github.com/yourusername/OpenProjectDataMining.git
```
2. Open the solution in Visual Studio 2022 / 2019
3. Restore NuGet packages
4. Configure your OpenProject API key in appsettings.json
5. Run the application:
```bash
dotnet run
```

## Future Improvements

- Integrate server-side filtering and search for large datasets
- Real-time dashboards with SignalR
- Export productivity reports to Excel / PDF
- Add user authentication and role management
- More advanced visualizations (Gantt charts, heatmaps)

## License: MIT


