import { useState } from 'react';
import './ProcessingModeSelector.css';

export default function ProcessingModeSelector({ selectedMode, onModeChange, customJson, onCustomJsonChange, geminiApiKey, onApiKeyChange }) {
    const [jsonError, setJsonError] = useState('');

    const handleJsonChange = (e) => {
        const value = e.target.value;
        onCustomJsonChange(value);

        // Validate JSON
        if (value.trim()) {
            try {
                JSON.parse(value);
                setJsonError('');
            } catch (err) {
                setJsonError('Invalid JSON format');
            }
        } else {
            setJsonError('');
        }
    };

    return (
        <div className="mode-selector">
            <h3>Processing Mode</h3>
            <div className="mode-options">
                <label className={`mode-option ${selectedMode === 'Auto' ? 'selected' : ''}`}>
                    <input
                        type="radio"
                        name="processingMode"
                        value="Auto"
                        checked={selectedMode === 'Auto'}
                        onChange={(e) => onModeChange(e.target.value)}
                    />
                    <div className="mode-content">
                        <span className="mode-icon">⚡</span>
                        <div>
                            <div className="mode-title">Auto Mode</div>
                            <div className="mode-description">Current AI-powered processing</div>
                        </div>
                    </div>
                </label>

                <label className={`mode-option ${selectedMode === 'Manual' ? 'selected' : ''}`}>
                    <input
                        type="radio"
                        name="processingMode"
                        value="Manual"
                        checked={selectedMode === 'Manual'}
                        onChange={(e) => onModeChange(e.target.value)}
                    />
                    <div className="mode-content">
                        <span className="mode-icon">✏️</span>
                        <div>
                            <div className="mode-title">Manual Mode</div>
                            <div className="mode-description">Provide custom JSON values</div>
                        </div>
                    </div>
                </label>

                <label className={`mode-option ${selectedMode === 'AIGenerated' ? 'selected' : ''}`}>
                    <input
                        type="radio"
                        name="processingMode"
                        value="AIGenerated"
                        checked={selectedMode === 'AIGenerated'}
                        onChange={(e) => onModeChange(e.target.value)}
                    />
                    <div className="mode-content">
                        <span className="mode-icon">🤖</span>
                        <div>
                            <div className="mode-title">AI Generated</div>
                            <div className="mode-description">Gemini API generates values</div>
                        </div>
                    </div>
                </label>
            </div>

            {selectedMode === 'Manual' && (
                <div className="mode-config manual-config">
                    <label className="config-label">
                        Custom JSON Values
                        <span className="hint">Example: {'{' + '"ClientName": "Acme Corp", "PolicyType": "Liability"' + '}'}</span>
                    </label>
                    <textarea
                        className="json-editor"
                        value={customJson}
                        onChange={handleJsonChange}
                        placeholder='{"ClientName": "Acme Corp", "PolicyType": "General Liability"}'
                        rows="6"
                    />
                    {jsonError && <div className="error-hint">{jsonError}</div>}
                </div>
            )}

            {selectedMode === 'AIGenerated' && (
                <div className="mode-config ai-config">
                    <label className="config-label">
                        Gemini API Key
                        <span className="hint">Get your key from Google AI Studio</span>
                    </label>
                    <input
                        type="password"
                        className="api-key-input"
                        value={geminiApiKey}
                        onChange={(e) => onApiKeyChange(e.target.value)}
                        placeholder="Enter your Gemini API key"
                    />
                </div>
            )}
        </div>
    );
}
