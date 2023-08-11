# ProjectSoft 📏

ProjectSoft is a collaborative project management solution which allows users to track tasks, assign tasks to team members, and set priority of tasks. It is written in C# and uses the [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/?view=netdesktop-7.0) UI framework as well as a Microsoft Azure SQL database.

## How to Build

To build ProjectSoft, use Visual Studio by and set .NET 7.0 as the target framework under `ProjectSoft > Properties > Target framework`.

![image](https://user-images.githubusercontent.com/103160379/232137783-35809591-bb74-4d9f-8eaa-ecdf8cb8b54d.png)

Then, change the build configuration to the `Release` setting.

![image](https://user-images.githubusercontent.com/103160379/232137649-5afcc77a-4062-4f29-97b3-c3c8e8045f75.png)

Build the program by going to `Build > Build ProjectSoft` in the context menu. Finally, the executable will appear in the project files under `/bin/Release/ProjectSoft.exe`.

## Code Organization

All of ProjectSoft's pages (home, projects, etc.) are organized in the home directory of the project. Fonts, icons, and other assets are contained within their own folder within the project.

## 3rd Party Libraries

ProjectSoft is dependent on the third-party WPF (Windows Presentation Foundation) UI framework and can be downloaded when installing Visual Studio.

## User Manual

The user manual for ProjectSoft can be found on our [GitBook](https://projectsoft.gitbook.io/projectsoft/).
