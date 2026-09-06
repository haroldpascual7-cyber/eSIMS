# System Requirements Specification (SRS)
## Smart School Incident Management System Using Rule-Based Algorithms for Incident Classification and Risk Assessment

---

## 1. System Overview & Core Philosophy
The **Smart School Incident Management System** is an intelligent, scalable school safety, incident monitoring, risk assessment, and disciplinary management platform. 

The core distinction of this platform is its **Rule-Based Algorithm Engine**, which transforms simple incident reporting into an automated decision-support pipeline:
`Incident Input -> Rule Engine Analysis -> Classification -> Risk Assessment -> Recommended Action -> Investigation -> Resolution -> Analytics`

---

## 2. User Roles & Role-Based Access Control (RBAC)

1. **Administrator (Admin)**: Full system access, user/staff/student management, category and rule configuration, report verification, system settings, backup/restore, and audit logs.
2. **Teacher / Staff**: Report incidents, upload evidence, track assigned incident follow-ups, submit observations, and receive system notifications.
3. **Student**: Submit incident reports (including anonymous reports), track status of own submissions, receive notifications, and view assigned restorative requirements.
4. **Guidance Counselor / Discipline Officer**: Review verified cases, conduct and document investigations, interview involved parties, recommend disciplinary/corrective actions, record counseling referrals, and monitor repeat offenders.
5. **Parent / Guardian (Optional/Integrated)**: View incident reports involving their child, acknowledge disciplinary notices, and track resolution statuses.

---

## 3. Main Dashboard & Analytics
- **KPI Overview Cards**: Total Incidents, Pending Verification, Under Investigation, Resolved, High-Risk, Critical, Incidents This Week, Incidents This Month.
- **Charts & Visualizations**:
  - Incidents by Category (Bullying, Theft, Altercation, etc.)
  - Severity & Risk Distribution (Low, Medium, High, Critical)
  - Grade/Year Level distribution
  - Location Analysis / Risk Heatmap (Classroom, Hallway, Playground, Cafeteria, etc.)
  - Monthly Trends & Resolution Rates
  - Repeat Incident Detection Alerts
- **Recent Activity Stream**: Real-time log of recently reported, investigated, and resolved incidents.

---

## 4. Incident Reporting & Evidence Management
- **Automated Incident ID**: Generated format (e.g., INC-2026-XXXXX).
- **Comprehensive Fields**: Date & Time, Specific Location, Category, Detailed Narrative, Reporter Identity (or Anonymous flag), Involved Students, Witnesses, Immediate Action Taken.
- **Incident Categories**: Bullying, Cyberbullying, Physical Altercation, Verbal Abuse, Harassment, Theft, Vandalism, Truancy, Fighting, Threats, Discrimination, Substance Misconduct, Unauthorized Access, Property Damage, Other.
- **Evidence Management**: Secure upload and association of photos, screenshots, documents, witness statements, and video links with access control.

---

## 5. Rule-Based Classification & Risk Assessment Engine

### A. Automatic Incident Classification
Analyzes narrative keywords, involved parties, location context, and incident category to determine:
- **Primary Category & Subcategory**
- **Calculated Severity Score**

### B. Rule-Based Risk Matrix
1. **Low Risk**: Minor classroom disruption, first-time minor misconduct, verbal disagreement.
   - *Recommended Response*: Verbal warning, teacher intervention, behavior monitoring.
2. **Medium Risk**: Repeated minor bullying, property damage, recurrent misconduct, severe verbal harassment.
   - *Recommended Response*: Parent notification, guidance counseling referral, preliminary investigation.
3. **High Risk**: Physical assault, explicit threats of harm, repeated severe bullying, targeted harassment.
   - *Recommended Response*: Immediate formal investigation, guidance/discipline referral, parent conference, administrative intervention.
4. **Critical Risk**: Serious violence, weapon possession, major emergency, immediate safety threat.
   - *Recommended Response*: Immediate escalation, administrative & security alerts, emergency safety procedures.

### C. Repeat Incident Detection Engine
- Automatically detects recurrent behavioral patterns (e.g., 3+ related infractions by a student within 30 days).
- Triggers **Repeat Incident Alerts** prompting mandatory guidance counseling or escalated disciplinary review.

---

## 6. Incident Lifecycle & Investigation Workflow

### A. Incident Status Lifecycle
`Reported -> Submitted -> Verified -> Under Investigation -> Action Taken -> Resolved -> Closed`

### B. Investigation Management
- Assign Lead Investigator (Guidance / Discipline Officer).
- Investigation Deadlines & Timeline Tracking.
- Interview Logs, Witness Statements, and Evidence Review.
- **Findings & Conclusions**: Substantiated, Unsubstantiated, Inconclusive, or Dismissed.

### C. Disciplinary Action & Counseling Tracking
- Types: Verbal Warning, Written Reprimand, Parent Conference, Counseling / Behavioral Contract, School Service, Suspension, Probation.
- Tracks responsible officer, scheduled dates, duration, completion status, and follow-up notes.

---

## 7. Reports, PDF Generation & Exporting
- **PDF Generation**: Official Incident Reports, Formal Investigation Summaries, Student Incident Transcripts, Monthly/Annual Administrative Summaries.
- **Report Customization**: Embeds institution logo, student information, classification, risk assessment, officer signatures, and final resolution.
- **Exporting**: Excel / CSV exports for administrative reporting and statistical analysis.

---

## 8. Security, Audit Trail & System Configuration

### A. Privacy & Security
- Secure password hashing, Role-Based Access Control (RBAC), and session expiration.
- Confidentiality levels on sensitive student and investigation files.

### B. Comprehensive Audit Trail
- Logs every critical action: user login/logout, incident creation, status updates, classification changes, risk score adjustments, evidence uploads, and report exports.

### C. Configurable System Settings
- Institution Branding: School Name, System Title, Motto, Tagline, Logo.
- Dynamic Configuration: Incident Categories, Risk Rule Thresholds, Locations, Departments, Grade Levels, User Accounts.
- Database Management: Local SQLite database with automated backup and restore facilities.

---

## 9. Entity Relationship & Data Model (Core Entities)
- Users & Roles
- Students & Staff
- IncidentReports
- IncidentCategories
- InvolvedPersons & Witnesses
- IncidentEvidence
- RiskRules & RiskAssessments
- Investigations & InvestigationNotes
- DisciplinaryActions
- Notifications
- AuditLogs
- Locations
- SystemSettings