import { useState } from 'react';
import DocumentUploader from './components/DocumentUploader';
import ProcessingStatus from './components/ProcessingStatus';
import { api } from './services/api';
import './App.css';

function App() {
  const [currentJobs, setCurrentJobs] = useState([]);
  const [completedJobs, setCompletedJobs] = useState([]);
  const [isUploading, setIsUploading] = useState(false);

  const handleUpload = async (files) => {
    setIsUploading(true);
    try {
      let response;

      if (files.length === 1) {
        response = await api.uploadDocument(files[0]);
        setCurrentJobs([response]);
      } else {
        response = await api.uploadBatch(files);
        setCurrentJobs(response.jobs);
      }
    } catch (error) {
      console.error('Upload error:', error);
      alert('Failed to upload documents. Make sure the API is running.');
    } finally {
      setIsUploading(false);
    }
  };

  const handleProcessingComplete = (jobs) => {
    setCompletedJobs(prev => [...prev, ...jobs]);
    setCurrentJobs([]);
  };

  return (
    <div className="app">
      <div className="background-gradient" />
      <div className="content">
        <header className="header">
          <h1>
            <span className="icon">⚡</span>
            Document Processing Engine
          </h1>
          <p className="subtitle">
            AI-powered template intelligence for Word documents
          </p>
        </header>

        <main className="main">
          {isUploading ? (
            <div className="loading">
              <div className="spinner" />
              <p>Uploading documents...</p>
            </div>
          ) : (
            <DocumentUploader onUpload={handleUpload} />
          )}

          {currentJobs.length > 0 && (
            <ProcessingStatus
              jobs={currentJobs}
              onComplete={handleProcessingComplete}
            />
          )}

          {completedJobs.length > 0 && (
            <div className="completed-section">
              <h3>Recently Completed</h3>
              <div className="completed-list">
                {completedJobs.map((job) => (
                  <div key={job.jobId} className="completed-card">
                    <div className="completed-header">
                      <span className="completed-icon">✓</span>
                      <span className="completed-filename">{job.fileName}</span>
                    </div>
                    <a
                      href={api.getDownloadUrl(job.jobId)}
                      download
                      className="download-btn"
                    >
                      Download
                    </a>
                  </div>
                ))}
              </div>
            </div>
          )}
        </main>

        <footer className="footer">
          <p>Powered by AI Template Intelligence • Built with React & ASP.NET Core</p>
        </footer>
      </div>
    </div>
  );
}

export default App;
