import React, { useState, useEffect } from 'react';
import axios from 'axios';

function StockPrices() {
    const [stockPrices, setStockPrices] = useState([]);

    useEffect(() => {
        axios.get('http://localhost:5261/api/stockprice')
            .then(response => {
                console.log('Data fetched:', response.data); // Log the fetched data
                const stockData = Array.isArray(response.data) ? response.data : [response.data];
                setStockPrices(stockData);
            })
            .catch(error => {
                console.error('There was an error fetching the stock prices!', error);
            });
    }, []);

    return (
        <div className="container mx-auto px-4 py-8">
            <h1 className="text-3xl font-bold mb-6 text-center">Stock Prices</h1>
            <div className="overflow-x-auto">
                <table className="min-w-full bg-white border border-gray-300">
                    <thead className="bg-gray-200">
                        <tr>
                            <th className="py-2 px-4 border-b">Symbol</th>
                            <th className="py-2 px-4 border-b">Price</th>
                            <th className="py-2 px-4 border-b">Timestamp</th>
                        </tr>
                    </thead>
                    <tbody>
                        {stockPrices.length > 0 ? (
                            stockPrices.map((stockPrice, index) => (
                                <tr key={index} className="text-center">
                                    <td className="py-2 px-4 border-b">{stockPrice.symbol}</td>
                                    <td className="py-2 px-4 border-b">{stockPrice.price}</td>
                                    <td className="py-2 px-4 border-b">{new Date(stockPrice.timestamp).toLocaleString()}</td>
                                </tr>
                            ))
                        ) : (
                            <tr>
                                <td colSpan="3" className="py-2 px-4 border-b text-center">No data available</td>
                            </tr>
                        )}
                    </tbody>
                </table>
            </div>
        </div>
    );
}

export default StockPrices;
