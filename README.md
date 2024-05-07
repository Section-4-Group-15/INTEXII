# INTEX 2 Project
## Overview
The INTEX 2 project is a comprehensive web application designed to offer a seamless online shopping experience for LEGO enthusiasts. Developed by Noah Haskett, Nya Croft, Jensen Hermansen, and Noah Hicks, this project showcases a robust and secure platform that leverages cutting-edge technologies and methodologies to enhance user interaction and safety. **This project came in 2nd place out of 60 groups** within the Information Systems program at the BYU Marriott School of Business 2023-24.
## Built With
- ASP.NET Core 8.0: Utilized for backend development, ensuring a powerful and efficient API.
- Entity Framework Core: Provides an advanced ORM framework synced with Azure SQL Database for fluid data operations.
- Microsoft Identity: Ensures robust authentication and user management.
- Azure Key Vault and Azure App Configuration: Secures application secrets and manages configuration effectively.
- Onnx Runtime: For implementing real-time machine learning predictions, enhancing both product recommendations and fraud detection.
## Features
### Security
- Implements OAuth with Google for secure and convenient user authentication.
- Enforces HTTPS, leveraging HSTS for enhanced security.
- Utilizes Azure Key Vault for secure storage of secrets and configuration in production.
### Machine Learning
- **Product Recommendations**: Personalized recommendations using machine learning to suggest items based on user preferences and behaviors.
- **Fraud Detection**: Real-time prediction of fraudulent transactions using a machine learning model, ensuring the integrity of transactions.
### User Roles
- **Admin Users**: Can manage products, users, and view detailed order information, including fraud predictions.
- **Authenticated Users**: Can make purchases, view orders, and manage their profiles.
- **Guest Users**: Can browse products and access general information without logging in.
## Setup and Installation
### Prerequisites
- .NET Core 8.0 SDK
- Azure Subscription (for deployment and services)
- Visual Studio 2019 or later
### Running Locally
- Clone the repository from GitHub.
- Open the solution in Visual Studio.
- Restore NuGet packages.
- Update the appsettings.json with your local settings.
- Run the application using IIS Express.
## Deployment
This application is deployed on Microsoft Azure, utilizing Azure App Services for web hosting and Azure SQL Database for data persistence. Steps for deployment include:

1. Set up Azure SQL Database and configure the connection strings.
2. Configure Azure App Services and optionally link to the GitHub repository for CI/CD.
3. Ensure all secrets and configuration settings are managed through Azure Key Vault or environment variables.
## Authors
- Noah Haskett
- Nya Croft
- Jensen Hermansen
- Noah Hicks

This project not only demonstrates our capability to build functional and secure web applications but also our commitment to applying industry-standard practices and advanced technologies to solve real-world problems.
