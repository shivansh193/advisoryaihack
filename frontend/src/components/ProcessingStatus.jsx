import { useEffect, useState } from 'react';
import { api } from '../services/api';
import './ProcessingStatus.css';

export default function ProcessingStatus({ jobs, onComplete }) {
    const [jobStatuses, setJobStatuses] = useState({});

    useEffect(() => {
        if (jobs.length === 0) return;

        const interval = setInterval(async () => {
            const updates = {};
            let allComplete = true;

            for (const job of jobs) {
                try {
                    const status = await api.getJobStatus(job.jobId);
                    updates[job.jobId] = status;

                    if (status.status !== 'Completed' && status.status !== 'Failed') {
                        allComplete = false;
                    }
                } catch (error) {
                    console.error('Error fetching job status:', error);
                }
            }

            setJobStatuses(updates);

            if (allComplete) {
                clearInterval(interval);
                onComplete?.(Object.values(updates));
            }
        }, 2000); // Poll every 2 seconds

        return () => clearInterval(interval);
    }, [jobs, onComplete]);

    if (jobs.length === 0) return null;

    return (
        <div className="processing-status">
            <h3>Processing Documents</h3>
            <div className="job-list">
                {jobs.map((job) => {
                    const status = jobStatuses[job.jobId] || job;
                    return (
                        <div key={job.jobId} className={`job-card ${status.status?.toLowerCase()}`}>
                            <div className="job-header">
                                <span className="job-filename">{job.fileName}</span>
                                <span className={`job-status-badge ${status.status?.toLowerCase()}`}>
                                    {status.status || 'Pending'}
                                </span>
                            </div>

                            <div className="job-progress">
                                <div
                                    className={`progress-bar ${status.status?.toLowerCase()}`}
                                    style={{
                                        width: status.status === 'Completed' ? '100%'
                                            : status.status === 'Processing' ? '50%'
                                                : status.status === 'Failed' ? '100%'
                                                    : '10%'
                                    }}
                                />
                            </div>

                            {status.errorMessage && (
                                <div className="error-message">{status.errorMessage}</div>
                            )}

                            {status.status === 'Completed' && status.downloadUrl && (
                                <a
                                    href={api.getDownloadUrl(job.jobId)}
                                    download
                                    className="download-link"
                                >
                                    Download Processed Document
                                </a>
                            )}
                        </div>
                    );
                })}
            </div>
        </div>
    );
}
