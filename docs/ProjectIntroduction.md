 
CAPSTONE PROJECT REGISTER


3. Register content of Capstone Project 
3.1. Capstone Project name: 
English: Building a Virtual Try-On Platform for Student Uniform E-Commerce with ASP.NET Core Web API, ReactJS and SQL Server
Vietnamese: Xây dựng hệ thống thử đồ ảo cho mua sắm đồng phục học sinh sử dụng công nghệ ASP.Net Core Web API, ReactJS và SQL Server
Abbreviation: VTOS
3.2. Context: (brief introduction)
Many students and parents struggle when purchasing school uniforms online due to uncertainties about size, fit, and overall appearance. These challenges often lead to incorrect orders, product returns, wasted time, and decreased user satisfaction. Schools and uniform suppliers also lack effective digital tools to showcase their products, manage catalogs, and analyze user needs. To address these issues, this project proposes the development of a web-based Virtual Try-On System that enables students and parents to preview uniforms directly on uploaded photos using image-processing, segmentation, and alignment techniques. The platform not only enhances the online shopping experience but also provides schools and vendors with uniform management features, analytics dashboards, and an integrated purchasing workflow. The objective of the project is to deliver a user-friendly, AI-supported system that reduces purchase friction, increases confidence in uniform selection, and supports data-driven decision-making for schools and uniform providers.
Objectives: 
 Develop a friendly and intuitive web platform for students to try on outfits virtually.
Apply image processing techniques to align clothes accurately on the uploaded person’s photo.
 Build a management system for users, outfits, and categories.

Technology/algorithm: 
Front-end: ReactJS
Back-end: C# (.NET Core / ASP.NET Core)
Database: SQL Server
Version Control: Github
IDE: Visual Studio 2022
CI/CD Tools: Azure DevOps / Azure Pipelines / GitHub Actions (Azure Platform)
Framework: .NET 8.0, Entity Framework, 

3.3. Summarize the contents to be researched and the expected outputs of the project:
The project will research and develop an AI-powered web application that enables students to virtually try on school uniforms and other outfits. The research focuses on applying image processing and machine learning techniques to overlay clothing images accurately on a user’s uploaded photo while ensuring natural alignment and visual realism
The expected outputs include:
A fully functional website for students to upload photos and virtually try on uniforms.
An admin dashboard for managing users, outfits, and categories.
A working AI image segmentation module integrated into the try-on feature.
Comprehensive documentation covering the system design, technical implementation, and research results.



3.4. Expected features

Parent
- User Registration and Login: Parents can create accounts and add one or more student which is their child
- Outfit Selection for Child: Parents can browse uniforms available for the child’s registered school.
- Search and Filtering of Outfits: Filters results by gender, grade, price range, or uniform type.
- Real-Time Try-On Preview for Child: Allows parents to preview how uniforms look on their child before purchase.
- Outfit Recommendation: Provides suggestions based on the child’s grade, gender, and previous selections.
- Try-On History Tracking per Child: Each child’s try-on history is recorded separately for easy comparison.
- Download Try-On Result: Parents can save try-on previews for future decision-making.
- User Feedback System: Parents can rate uniforms and provide feedback for product or visual quality.
- Payment Integration: Secure checkout process using integrated payment gateways such as VNPay or MoMo.
- Order Tracking: Parents can view the status of uniform orders, including payment confirmation and delivery tracking.

Admin
- Admin User Management: Manage accounts for students, parents, and schools. Approve or suspend accounts as needed.
- Admin Outfit Management: Add, edit, or remove outfit entries. Upload images, assign schools, and categorize by gender, grade, or type.
- Image and File Management: Centralized file library for uniform images and promotional content, with version control.
- Security and Privacy Enforcement: Ensure data encryption, access control, and compliance with privacy regulations.
- Performance Optimization: Utilize caching, CDN, and asynchronous loading to improve system responsiveness.
- Report Generation: Generate system-wide reports on user activity, try-on frequency, and feedback ratings.
- Moderate User Feedback: Review, approve, or delete inappropriate comments or reviews.
- Configure Outfit Recommendation Rules: Fine-tune the AI algorithm parameters (e.g., grade-weighted suggestions, popularity scores).
- System Configuration: Manage integrations, email notifications, payment gateways, and permissions for other actors.
- Dashboard Analytics: Interactive dashboard showing metrics such as number of users, try-on sessions, sales volume, and top-rated outfits.

School
- School Profile Management: Schools can manage their official information including name, logo, contact, and catalog.
- Upload & Manage Official School Uniforms: Upload uniform images, define variants, and maintain product descriptions.
- Maintain Outfit Metadata: Store details such as size charts, material type, washing instructions, and color standards.
- Update Pricing / Stock / Availability: Schools can edit stock quantities, pricing, and mark certain items as “out of stock.”
- View Feedback Related to Their Uniforms: Schools can monitor user reviews and ratings for continuous improvement.
- Generate Sales and Feedback Reports: Receive periodic reports summarizing purchases, ratings, and engagement.
- Management Student: Add, edit or delete student information like fullname, ages, phone numbers of parent, grade, avatar.

Supplier
- View Production Batches: View the list of new production orders sent by schools that are waiting for approval.
- Approve Production Batch: Confirm acceptance of a production batch to begin the fulfillment and manufacturing process.
- Reject Production Batch: Decline a production order request if the supplier cannot meet the required conditions or capacity.
- Update Batch Status: Update and track the current processing stage of each production batch (e.g., in production, completed)
- Handover Batch to School: Confirm that production has been completed and the finished goods have been delivered to the school.
- View Batch History: Review the history of completed, delivered, or rejected production batches.
- Update Supplier Profile: Edit supplier contact information, address, and business capacity details.

Payment
- Payment Integration (VNPay, MoMo, etc.): Multi-gateway support for local payment methods, with real-time transaction status updates.
- Order Creation and Checkout Process: Users can add uniforms to a cart and complete purchases via a unified checkout interface.
- Payment Verification & Webhook Handling: Automatically confirms successful transactions and updates order status.
- Transaction Logging and History: Keeps a full log of all payments, including order ID, payer info, and timestamps.
- Receipt / Invoice Generation: Generates digital receipts and invoices downloadable by users.
- Refund & Dispute Handling: Supports refund requests and dispute resolution workflows.
- Payment Report Summary for Admin & School: Consolidated financial reports for administrators and schools to track revenue.















