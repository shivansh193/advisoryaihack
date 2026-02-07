import { useState, useRef } from 'react';
import './DocumentUploader.css';

export default function DocumentUploader({ onUpload }) {
    const [selectedFiles, setSelectedFiles] = useState([]);
    const [isDragging, setIsDragging] = useState(false);
    const fileInputRef = useRef(null);

    const handleDragOver = (e) => {
        e.preventDefault();
        setIsDragging(true);
    };

    const handleDragLeave = (e) => {
        e.preventDefault();
        setIsDragging(false);
    };

    const handleDrop = (e) => {
        e.preventDefault();
        setIsDragging(false);

        const files = Array.from(e.dataTransfer.files).filter(file =>
            file.name.endsWith('.docx')
        );

        if (files.length > 0) {
            setSelectedFiles(files);
        } else {
            alert('Please drop only .docx files');
        }
    };

    const handleFileSelect = (e) => {
        const files = Array.from(e.target.files);
        setSelectedFiles(files);
    };

    const handleUpload = () => {
        if (selectedFiles.length === 0) {
            alert('Please select at least one file');
            return;
        }

        onUpload(selectedFiles);
        setSelectedFiles([]);
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    const removeFile = (index) => {
        setSelectedFiles(files => files.filter((_, i) => i !== index));
    };

    return (
        <div className="uploader-container">
            <div
                className={`drop-zone ${isDragging ? 'dragging' : ''}`}
                onDragOver={handleDragOver}
                onDragLeave={handleDragLeave}
                onDrop={handleDrop}
                onClick={() => fileInputRef.current?.click()}
            >
                <div className="drop-icon">📄</div>
                <h3>Drag & Drop Word Documents</h3>
                <p>or click to browse</p>
                <span className="file-type-hint">Only .docx files accepted</span>
            </div>

            <input
                ref={fileInputRef}
                type="file"
                accept=".docx"
                multiple
                onChange={handleFileSelect}
                style={{ display: 'none' }}
            />

            {selectedFiles.length > 0 && (
                <div className="selected-files">
                    <h4>Selected Files ({selectedFiles.length})</h4>
                    <div className="file-list">
                        {selectedFiles.map((file, index) => (
                            <div key={index} className="file-item">
                                <span className="file-name">{file.name}</span>
                                <span className="file-size">
                                    {(file.size / 1024).toFixed(1)} KB
                                </span>
                                <button
                                    className="remove-btn"
                                    onClick={() => removeFile(index)}
                                    aria-label="Remove file"
                                >
                                    ×
                                </button>
                            </div>
                        ))}
                    </div>

                    <button className="upload-btn" onClick={handleUpload}>
                        Process {selectedFiles.length} Document{selectedFiles.length > 1 ? 's' : ''}
                    </button>
                </div>
            )}
        </div>
    );
}
