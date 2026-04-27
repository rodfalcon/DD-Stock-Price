import React from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import App from './App';
import reportWebVitals from './reportWebVitals';
import { datadogRum } from '@datadog/browser-rum';
import { datadogLogs } from '@datadog/browser-logs';

const ddApplicationId =
  process.env.REACT_APP_DD_APPLICATION_ID || 'c3760513-419d-430d-8555-39e8e332d818';
const ddClientToken =
  process.env.REACT_APP_DD_CLIENT_TOKEN || 'pubb32205f55611319be86fad6ef509e301';
const ddSite = process.env.REACT_APP_DD_SITE || 'datadoghq.com';
const ddEnv = process.env.REACT_APP_DD_ENV || 'dev';
const ddVersion = process.env.REACT_APP_DD_VERSION || '1.0.0';

// RUM injects trace headers only when a rule matches the URL the instrumented client uses.
// Regexes like /^\/api\// never match full URLs (they start with "http"), which caused flaky / single-span traces.
function tracingUrlForMatch(input) {
  if (typeof input === 'string') return input;
  if (input && typeof input === 'object') {
    if (typeof input.url === 'string') return input.url;
    if (typeof input.href === 'string') return input.href;
  }
  return '';
}

const allowedTracingUrls = [
  (candidate) => {
    const raw = tracingUrlForMatch(candidate);
    if (!raw) return false;
    if (typeof window === 'undefined' || !window.location?.origin) {
      return raw.includes('/api/');
    }
    try {
      const u = new URL(raw, window.location.origin);
      return u.origin === window.location.origin && u.pathname.startsWith('/api/');
    } catch {
      return raw.startsWith('/api/') || raw.includes('/api/');
    }
  },
];

datadogRum.init({
  applicationId: ddApplicationId,
  clientToken: ddClientToken,
  site: ddSite,
  service: 'stock-price-frontend',
  env: ddEnv,
  version: ddVersion,
  sessionSampleRate: 100,
  sessionReplaySampleRate: 100,
  trackUserInteractions: true,
  trackResources: true,
  trackLongTasks: true,
  defaultPrivacyLevel: 'mask-user-input',
  allowedTracingUrls,
  // Default is "sampled"; that can skip injecting context so the API starts a new trace. Force injection for linking.
  traceContextInjection: 'all',
});

datadogLogs.init({
  clientToken: ddClientToken,
  site: ddSite,
  service: 'stock-price-frontend',
  env: ddEnv,
  forwardErrorsToLogs: true,
  sampleRate: 100,
});

const container = document.getElementById('root');
const root = createRoot(container);

root.render(
  <React.StrictMode>
    <App />
  </React.StrictMode>
);

reportWebVitals();
