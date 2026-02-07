import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7062/api';

export const api = {
  async uploadDocument(file) {
    const formData = new FormData();
    formData.append('file', file);

    const response = await axios.post(`${API_BASE_URL}/documents/upload`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });

    return response.data;
  },

  async uploadBatch(files) {
    const formData = new FormData();
    files.forEach(file => {
      formData.append('files', file);
    });

    const response = await axios.post(`${API_BASE_URL}/documents/batch`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });

    return response.data;
  },

  async getJobStatus(jobId) {
    const response = await axios.get(`${API_BASE_URL}/documents/jobs/${jobId}`);
    return response.data;
  },

  getDownloadUrl(jobId) {
    return `${API_BASE_URL}/documents/jobs/${jobId}/download`;
  },

  async downloadDocument(jobId) {
    const response = await axios.get(`${API_BASE_URL}/documents/jobs/${jobId}/download`, {
      responseType: 'blob',
    });

    return response.data;
  },
};
