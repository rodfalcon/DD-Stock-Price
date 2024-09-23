// App.js
import React from 'react';
import './App.css';
import StockPrices from './components/StockPrices';

function App() {
  return (
    <div className="App">
      <header className="App-header">
        <h1>Stock Prices Dashboard</h1>
      </header>
      <StockPrices />
    </div>
  );
}

export default App;
