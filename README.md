# Document Processing Application

AI-powered Word document template processing with a modern web interface.

## 🚀 Quick Start

### Backend API
```powershell
cd c:\Development\advisoryai\TemplateEngine.Api
dotnet run
```
API runs at: `https://localhost:7062`

### Frontend
```powershell
cd c:\Development\advisoryai\frontend
npm run dev
```
Frontend runs at: `http://localhost:5173`

## 📝 Features

- **Drag & Drop Upload** - Upload single or multiple .docx files
- **Real-time Processing** - Watch documents process with live status updates
- **AI-Powered** - Intelligent template detection and content generation
- **Modern UI** - Premium dark theme with glassmorphism effects
- **Batch Processing** - Process multiple documents simultaneously

## 📚 Documentation

See [walkthrough.md](file:///C:/Users/Shivansh%20Kalra/.gemini/antigravity/brain/d3786701-34da-43cc-8e8d-0e5ca5e3d405/walkthrough.md) for complete documentation.

## 🏗️ Architecture

- **Backend**: ASP.NET Core Web API (.NET 9)
- **Frontend**: React 18 + Vite
- **Processing**: OpenXML SDK + Microsoft Semantic Kernel

## 🔧 Development

API Endpoint: `https://localhost:7062/api`
- POST `/api/documents/upload` - Upload single file
- POST `/api/documents/batch` - Upload multiple files
- GET `/api/documents/jobs/{id}` - Check status
- GET `/api/documents/jobs/{id}/download` - Download result

## 📦 Project Structure

```
TemplateEngine.Api/      # ASP.NET Core API
TemplateEngine.Core/     # Document processing engine
frontend/                # React UI
```

**Built with ❤️ using React & ASP.NET Core**
