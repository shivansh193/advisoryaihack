import { useState } from 'react';
import './PlaceholderEditor.css';

export default function PlaceholderEditor({ jobId, fileName, placeholders, onProcess }) {
    const [mode, setMode] = useState('manual');
    const [values, setValues] = useState(placeholders);
    const [geminiApiKey, setGeminiApiKey] = useState('');
    const [isProcessing, setIsProcessing] = useState(false);

    const handleValueChange = (key, newValue) => {
        setValues(prev => ({ ...prev, [key]: newValue }));
    };

    const handleProcess = async () => {
        setIsProcessing(true);
        try {
            await onProcess(jobId, mode, values, geminiApiKey);
        } finally {
            setIsProcessing(false);
        }
    };

    const handleAIGenerate = async () => {
        if (!geminiApiKey.trim()) {
            alert('Please enter your Gemini API key');
            return;
        }

        setIsProcessing(true);
        try {
            await onProcess(jobId, 'ai', null, geminiApiKey);
        } finally {
            setIsProcessing(false);
        }
    };

    return (
        <div className="placeholder-editor">
            <div className="editor-header">
                <h3>📄 {fileName}</h3>
                <p className="detected-count">Detected {Object.keys(placeholders).length} placeholder(s)</p>
            </div>

            <div className="mode-selector-compact">
                <label className={mode === 'manual' ? 'active' : ''}>
                    <input
                        type="radio"
                        name="editorMode"
                        value="manual"
                        checked={mode === 'manual'}
                        onChange={() => setMode('manual')}
                    />
                    <span>✏️ Manual Edit</span>
                </label>
                <label className={mode === 'ai' ? 'active' : ''}>
                    <input
                        type="radio"
                        name="editorMode"
                        value="ai"
                        checked={mode === 'ai'}
                        onChange={() => setMode('ai')}
                    />
                    <span>🤖 AI Generate</span>
                </label>
            </div>

            {mode === 'manual' ? (
                <div className="placeholder-list">
                    {Object.entries(values).map(([key, value]) => (
                        <div key={key} className="placeholder-item">
                            <label className="placeholder-label">{key}</label>
                            <input
                                type="text"
                                className="placeholder-input"
                                value={value}
                                onChange={(e) => handleValueChange(key, e.target.value)}
                                placeholder={`Enter value for ${key}`}
                            />
                        </div>
                    ))}
                    <button
                        className="process-btn"
                        onClick={handleProcess}
                        disabled={isProcessing}
                    >
                        {isProcessing ? 'Processing...' : '✨ Process Document'}
                    </button>
                </div>
            ) : (
                <div className="ai-mode">
                    <div className="api-key-section">
                        <label>Gemini API Key</label>
                        <input
                            type="password"
                            className="api-key-input"
                            value={geminiApiKey}
                            onChange={(e) => setGeminiApiKey(e.target.value)}
                            placeholder="Enter your Gemini API key"
                        />
                        <a
                            href="https://aistudio.google.com/apikey"
                            target="_blank"
                            rel="noopener noreferrer"
                            className="get-key-link"
                        >
                            Get API Key →
                        </a>
                    </div>

                    <div className="placeholder-preview">
                        <h4>Detected Placeholders:</h4>
                        <ul>
                            {Object.keys(placeholders).map(key => (
                                <li key={key}>{key}</li>
                            ))}
                        </ul>
                    </div>

                    <button
                        className="process-btn ai-btn"
                        onClick={handleAIGenerate}
                        disabled={isProcessing || !geminiApiKey.trim()}
                    >
                        {isProcessing ? 'Generating...' : '🤖 Generate with AI & Process'}
                    </button>
                </div>
            )}
        </div>
    );
}
