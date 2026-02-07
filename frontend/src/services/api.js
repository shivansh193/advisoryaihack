import axios from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'https://localhost:7062/api';

export const api = {
  async uploadDocument(file, config = {}) {
    const formData = new FormData();
    formData.append('file', file);

    if (config.processingMode) {
      formData.append('processingMode', config.processingMode);
    }
    if (config.customJson) {
      formData.append('customJson', config.customJson);
    }
    if (config.geminiApiKey) {
      formData.append('geminiApiKey', config.geminiApiKey);
    }

    const response = await axios.post(`${API_BASE_URL}/documents/upload`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });

    return response.data;
  },

  async uploadBatch(files, config = {}) {
    const formData = new FormData();
    files.forEach(file => {
      formData.append('files', file);
    });

    if (config.processingMode) {
      formData.append('processingMode', config.processingMode);
    }
    if (config.customJson) {
      formData.append('customJson', config.customJson);
    }
    if (config.geminiApiKey) {
      formData.append('geminiApiKey', config.geminiApiKey);
    }

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

  async analyzeDocument(file) {
    const formData = new FormData();
    formData.append('file', file);

    const response = await axios.post(`${API_BASE_URL}/documents/analyze`, formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });

    return response.data;
  },

  async processDocument(jobId, mode, values, geminiApiKey) {
    const response = await axios.post(
      `${API_BASE_URL}/documents/process/${jobId}`,
      {
        mode,
        values,
        geminiApiKey
      },
      {
        responseType: 'blob',
      }
    );

    return response.data;
  },
};
