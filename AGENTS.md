# Agent Context: Smart School Incident Management System

## Architectural Rules
- **Modularity**: No single file shall exceed **500 lines of code**.
- **Backend-First**: Decoupled data layer and service abstractions.
- **Data Persistence**: Local **SQLite** database via Entity Framework Core (`AppDbContext`).
- **Sync-Ready Model Design**: Every entity inherits `EntityBase`:
    - `Guid SyncId`: Unique identifier for future cloud synchronization.
    - `DateTime CreatedAt`: UTC creation timestamp.
    - `DateTime UpdatedAt`: UTC update timestamp.
    - `bool IsSynced`: Flag tracking sync status (remote sync execution deferred).
- **Authentication & RBAC**: `IAuthService` & `AuthService` verifying against SQLite `Users` table.
- **Theming**: Dynamic **Dark and Light modes** using `ThemeService` and XAML Resource Dictionaries.
- **Kiosk Presentation**: Full-screen kiosk layout with discrete administrative window management controls.
- **System Requirements Specification**: Tracked comprehensively in `SYSTEM_REQUIREMENTS.md`.

## Strict Theming & Color Rules (Zero Hardcoded Colors)
- **Zero Hardcoded Colors**: UI components, views, modals, forms, user controls, containers, borders, and text MUST NEVER use hardcoded color values (such as `#FFFFFF`, `#000000`, `White`, `Black`, `#F8FAFC`, `#0F172A`, `#1E293B`, `#64748B`, `#334155`, `#CBD5E1`, `#E2E8F0`, etc.) or static color references.
- **Mandatory DynamicResource**: Always bind color properties (`Background`, `Foreground`, `BorderBrush`, `CaretBrush`, `Fill`, `Stroke`) using `{DynamicResource <ThemeBrushKey>}` so the UI responds dynamically and instantaneously to runtime theme switches (Light/Dark mode).
    - **Backgrounds**: `AppBackgroundBrush`, `SurfaceBackgroundBrush`, `CardBackgroundBrush`, `InputBackgroundBrush`, `HeaderBackgroundBrush`, `TableRowAltBrush`, `TableHoverBrush`
    - **Text & Foregrounds**: `PrimaryTextBrush`, `SecondaryTextBrush`, `MutedTextBrush`, `ButtonTextBrush`
    - **Borders & Dividers**: `BorderBrush`, `InputBorderBrush`, `DividerBrush`
    - **Accents & Brands**: `PrimaryBrush`, `PrimaryHoverBrush`, `PrimaryActiveBrush`, `AccentBrush`
    - **Badges & Status**: `SuccessBrush`, `SuccessBgBrush`, `WarningBrush`, `WarningBgBrush`, `DangerBrush`, `DangerBgBrush`, `InfoBrush`, `InfoBgBrush`
- **Form Controls & Inputs**: All input elements (`TextBox`, `PasswordBox`, `ComboBox`, `DataGrid`, `ListBox`) must utilize dynamic theme styling (`ModernTextBoxStyle`, `ModernComboBoxStyle`, or `{DynamicResource InputBackgroundBrush}` and `{DynamicResource PrimaryTextBrush}`) to guarantee complete readability and responsiveness across both Light and Dark themes.
- **Proactive Remediation ("If Seen, Change It")**: Whenever inspecting, modifying, or creating any XAML view, modal dialog, or form, check for any hardcoded colors or theme-unresponsive elements. If any hardcoded color is detected, immediately refactor it to use the appropriate `{DynamicResource}` token.

## Core Pipeline (Rule-Based Decision Support)
Incident Input -> Rule Engine Analysis -> Classification -> Risk Assessment -> Recommended Action -> Investigation -> Resolution -> Analytics

## Project Status
- [x] Project initialized (.NET 8.0 WPF)
- [x] Directory structure configured
- [x] SQLite database schema & models (`Student`, `Staff`, `User`, `Incident`, `SystemSetting`, `Enums`)
- [x] Database initial seeding (`DbInitializer`)
- [x] Repository pattern implementation (`IRepository<T>`, `Repository<T>`)
- [x] Incident Service layer (`IIncidentService`, `IncidentService`)
- [x] System Settings Service (`ISettingsService`, `SettingsService`)
- [x] Authentication Service (`IAuthService`, `AuthService`)
- [x] Theme Management (`IThemeService`, `ThemeService`, `LightTheme.xaml`, `DarkTheme.xaml`, `CoreStyles.xaml`)
- [x] MVVM Foundation (`ObservableObject`, `RelayCommand`)
- [x] Split-Screen Kiosk Login (`LoginView`, `LoginViewModel`)
- [x] Dashboard & Incidents Management Views (`DashboardView`, `IncidentListView`, `IncidentDetailView`, `MainWindow`)
- [x] SRS Specification Document (`SYSTEM_REQUIREMENTS.md`)
- [x] Build verified (0 warnings, 0 errors)

## Directory Structure
- `SYSTEM_REQUIREMENTS.md`: System Requirements Specification & feature blueprint.
- `Models/`: Entity definitions (`EntityBase`, `Student`, `Staff`, `User`, `Incident`, `SystemSetting`, `Enums`).
- `Data/`: SQLite `AppDbContext` and `DbInitializer`.
- `Repositories/`: Generic data access layer (`IRepository<T>`, `Repository<T>`).
- `Services/`: Business logic (`IIncidentService`, `IncidentService`), Auth (`IAuthService`, `AuthService`), Settings (`ISettingsService`, `SettingsService`), and Theme (`IThemeService`, `ThemeService`).
- `ViewModels/`: MVVM ViewModels (`LoginViewModel`, `DashboardViewModel`, `IncidentListViewModel`, `IncidentDetailViewModel`, `MainViewModel`, `Common/`).
- `Views/`: XAML Views and code-behind (`LoginView`, `DashboardView`, `IncidentListView`, `IncidentDetailView`).
- `Styles/`: XAML Resource Dictionaries (`LightTheme.xaml`, `DarkTheme.xaml`, `CoreStyles.xaml`).