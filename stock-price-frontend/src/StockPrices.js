import React, { useState, useEffect } from 'react';
import axios from 'axios';

function StockPrices() {
    const [stockPrices, setStockPrices] = useState([]);

    useEffect(() => {
        axios.get('http://localhost:5261/api/stockprice')
            .then(response => {
                // Log the response data to verify the structure
                console.log('Data fetched:', response.data);

                // Convert the response data to an array format expected by the table
                const stockData = Array.isArray(response.data) ? response.data : [response.data];
                setStockPrices(stockData);
            })
            .catch(error => {
                console.error('There was an error fetching the stock prices!', error);
            });
    }, []);

    return (
        <div>
            <h1>Stock Prices</h1>
            <table>
                <thead>
                    <tr>
                        <th>Symbol</th>
                        <th>Price</th>
                        <th>Timestamp</th>
                    </tr>
                </thead>
                <tbody>
                    {stockPrices.length > 0 ? (
                        stockPrices.map((stockPrice, index) => (
                            <tr key={index}>
                                <td>{stockPrice.symbol}</td>
                                <td>{stockPrice.price}</td>
                                <td>{new Date(stockPrice.timestamp).toLocaleString()}</td>
                            </tr>
                        ))
                    ) : (
                        <tr>
                            <td colSpan="3">No data available</td>
                        </tr>
                    )}
                </tbody>
            </table>
        </div>
    );
}

export default StockPrices;
