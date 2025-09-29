# SkillBridge

Connecting Investors and Innovation together here in SkillBridge


## Team Members : 

| Name | Roll Number | E-mail | Role |
|-------------|--------------|--------------|--------------|
| Kazi Kamruddin Ahmed  | 20220104125 | kazikamruddinahmed@gmail.com| Lead |
| Rakibul Islam Rahi  | 20210204077 | rakibulislam.rahi.rir@gmail.com | FrontEnd |
| Md. Abdullah Istiaq  | 20220104118 | ishtiaqsakib@gmail.com | FrontEnd |


## Project Overview
SkillBridge is a platform where people exchange **skills and knowledge** directly.  
Users can offer to teach skills they know, request to learn skills from others, and collaborate through structured interactions, conversations, and communities.  


## Title
### **SkillBridge**

## Short Description
SkillBridge empowers peer-to-peer learning by connecting users who want to learn with those who want to teach. The system manages skill categories, requests, interactions, and communication — making knowledge exchange simple, secure, and community-driven.  


## Key Features

1. **User Profiles**  
   Users register with details like full name, profession, bio, and location. Skills are linked with proficiency stages.

2. **Skill Management**  
   - Add skills under categories (e.g., Programming, Design, Marketing).  
   - Define stages for structured learning.  
   - Track progress of learners by stages.  

3. **Skill Requests & Interactions**  
   - Learners send requests to teachers for specific skills.  
   - Requests evolve into interactions with multiple sessions.  
   - Each session has confirmation checkpoints for both parties.  

4. **Communities & Posts**  
   - Join skill-based communities.  
   - Share posts, ask questions, and comment to build collaborative discussions.  

5. **Conversations & Messaging**  
   - Real-time private chat between two users.  
   - Messages stored securely with encryption fields (Ciphertext, IV, HMAC).  

6. **Notifications**  
   - Get notified when receiving requests, session confirmations, or new messages.  

7. **Ratings & Feedback**  
   - After interactions, users can rate each other and leave comments.  
   - Ratings accumulate into a user’s reputation score.  


## Target Audience

1. **Learners**  
   - Individuals who want to gain new skills with structured guidance.  

2. **Teachers / Skill Sharers**  
   - People with expertise who want to share knowledge and mentor others.  

3. **Communities**  
   - Groups centered around specific skills where collaboration and networking thrive.  


## Tech Stack

### **1. Backend**
- **Framework**: ASP.NET Core / C#  
  Handles authentication, APIs, skill/interaction logic, and messaging encryption.

### **2. Frontend**
- **Framework**: React  
  Provides dynamic UI for managing skills, communities, and conversations.

### **3. Rendering Method**
- **Client-Side Rendering (CSR)** for interactive updates and real-time features.  

### **4. Database**
- **SQL Server** (MSSQL)  
  Stores users, skills, communities, conversations, requests, and ratings.  


## User Interface (Planned)

- Dashboard for learners & teachers.  
- Communities with posts & comments.  
- Messaging panel for real-time chats.  
- Ratings and session history visualization.  


## Project Features (Schema-based)

### **1. User Authentication**
- Secure login & registration with ASP.NET Identity (`AspNetUsers`).  

---

### **2. Skill Management**
- Skills categorized into **SkillCategories** and **SkillStages**.  
- Users register their skills (`UserSkills`) with progress tracking.  

---

### **3. Requests & Interactions**
- Learners send **SkillRequests** to teachers.  
- Approved requests create **Interactions** and **InteractionSessions**.  
- Sessions progress step-by-step until completion.  

---

### **4. Communities**
- Each skill has a **Community**.  
- Communities contain **Posts** and **Comments** (`CommunityPosts`, `CommunityComments`).  

---

### **5. Conversations & Messaging**
- **Conversations** between two users.  
- Encrypted **Messages** with read receipts.  

---

### **6. Notifications**
- Alerts for requests, new posts, messages, and updates (`Notifications`).  

---

### **7. Ratings & Reputation**
- Users rate each other after completing sessions (`Ratings`, `UserRatings`).  
- Accumulated rating builds credibility.  


## Milestones
### Milestone 1: Core System
- Implement **User Authentication**, **Skill Management**, and **Requests**.

### Milestone 2: Interactions & Communication
- Add **Interaction Sessions**, **Conversations**, and **Notifications**.

### Milestone 3: Community & Reputation
- Launch **Communities**, **Posts/Comments**, and **User Ratings** system.
- UI polish and deployment.




