import React from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App';
import reportWebVitals from './reportWebVitals';
import { datadogRum } from '@datadog/browser-rum';
import { datadogLogs } from '@datadog/browser-logs';
import log from 'loglevel';

datadogRum.init({
    applicationId: 'c3760513-419d-430d-8555-39e8e332d818',
    clientToken: 'pubb32205f55611319be86fad6ef509e301',
    site: 'datadoghq.com',
    service: 'stock-price-frontend',
    env: 'dev',
    version: '1.0.0',
    sessionSampleRate: 100,
    sessionReplaySampleRate: 100,
    trackUserInteractions: true,
    trackResources: true,
    trackLongTasks: true,
    defaultPrivacyLevel: 'mask-user-input',
    allowedTracingUrls: ["http://localhost:5261/api/stockprice", /https:\/\/.*\.my-api-domain\.com/, (url) => url.startsWith("http://localhost:5261/api/stockprice")]
});

datadogLogs.init({
  clientToken: 'pubb32205f55611319be86fad6ef509e3',
  site: 'datadoghq.com',
  forwardErrorsToLogs: true,
  sampleRate: 100,
});

log.setLevel('info');  // Set the log level

log.info('This is an info message');
log.error('This is an error message');

const container = document.getElementById('root');
const root = createRoot(container);

root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

reportWebVitals();
