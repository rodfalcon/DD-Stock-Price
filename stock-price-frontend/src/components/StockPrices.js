import React, { useState, useEffect } from 'react';
import axios from 'axios';

const StockPrices = () => {
    const [datadogData, setDatadogData] = useState([]);  // Start as empty array

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        try {
            const response = await axios.get('http://localhost:5261/api/stockprice/DDOG');  // Your backend API
            console.log('Datadog data:', response.data);  // Logging the fetched data

            // Always wrap object in an array
            if (response.data) {
                const arrayData = Array.isArray(response.data) ? response.data : [response.data];
                setDatadogData(arrayData);  // Update state with array
            } else {
                console.log('API response is empty or invalid');
            }
        } catch (error) {
            console.error("Error fetching Datadog data:", error);
        }
    };

    return (
        <div>
            <h2>Stock Prices Dashboard</h2>
            {datadogData.length > 0 ? (
                <table border="1" cellPadding="10">
                    <thead>
                        <tr>
                            <th>Symbol</th>
                            <th>Price</th>
                            <th>Open</th>
                            <th>High</th>
                            <th>Low</th>
                            <th>Change</th>
                            <th>Change Percent</th>
                            <th>Volume</th>
                            <th>Timestamp</th>
                        </tr>
                    </thead>
                    <tbody>
                        {datadogData.map((data, index) => (
                            <tr key={index}>
                                <td>{data.symbol || 'N/A'}</td>
                                <td>{data.price || 'N/A'}</td>
                                <td>{data.open || 'N/A'}</td>
                                <td>{data.high || 'N/A'}</td>
                                <td>{data.low || 'N/A'}</td>
                                <td>{data.change || 'N/A'}</td>
                                <td>{data.changePercent || 'N/A'}</td>
                                <td>{data.volume || 'N/A'}</td>
                                <td>{data.timestamp ? new Date(data.timestamp).toLocaleString() : 'N/A'}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            ) : (
                <p>No data available yet...</p>  // Default message if no data
            )}
        </div>
    );
};

export default StockPrices;
