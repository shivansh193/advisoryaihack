import { useState } from 'react';
import DocumentUploader from './components/DocumentUploader';
import PlaceholderEditor from './components/PlaceholderEditor';
import { api } from './services/api';
import './App.css';

function App() {
  const [analyzeResult, setAnalyzeResult] = useState(null);
  const [isAnalyzing, setIsAnalyzing] = useState(false);

  const handleUpload = async (files) => {
    if (files.length === 0) return;

    setIsAnalyzing(true);
    try {
      // Only process first file for now
      const result = await api.analyzeDocument(files[0]);
      setAnalyzeResult(result);
    } catch (error) {
      console.error('Analyze error:', error);
      alert('Failed to analyze document. Make sure the API is running.');
    } finally {
      setIsAnalyzing(false);
    }
  };

  const handleProcess = async (jobId, mode, values, geminiApiKey) => {
    try {
      const blob = await api.processDocument(jobId, mode, values, geminiApiKey);

      // Download processed file
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `Processed_${analyzeResult.fileName}`;
      document.body.appendChild(a);
      a.click();
      window.URL.revokeObjectURL(url);
      document.body.removeChild(a);

      // Reset for next upload
      setAnalyzeResult(null);
      alert('✅ Document processed and downloaded successfully!');
    } catch (error) {
      console.error('Process error:', error);
      alert('Failed to process document: ' + (error.response?.data?.error || error.message));
    }
  };

  const handleReset = () => {
    setAnalyzeResult(null);
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
            Two-Phase AI-powered document processing
          </p>
        </header>

        <main className="main">
          {isAnalyzing ? (
            <div className="loading">
              <div className="spinner" />
              <p>Analyzing document...</p>
            </div>
          ) : analyzeResult ? (
            <>
              <PlaceholderEditor
                jobId={analyzeResult.jobId}
                fileName={analyzeResult.fileName}
                placeholders={analyzeResult.detectedPlaceholders}
                onProcess={handleProcess}
              />
              <button className="reset-btn" onClick={handleReset}>
                ← Upload Another Document
              </button>
            </>
          ) : (
            <DocumentUploader onUpload={handleUpload} />
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
