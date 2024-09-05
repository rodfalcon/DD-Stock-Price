import React, { useState, useEffect } from 'react';
import axios from 'axios';

const StockPrices = () => {
    const [datadogData, setDatadogData] = useState([]); // Initialize as empty arrays
    const [dynatraceData, setDynatraceData] = useState([]);
    const [newRelicData, setNewRelicData] = useState([]);
    const [showDynatrace, setShowDynatrace] = useState(false);
    const [showNewRelic, setShowNewRelic] = useState(false);

    useEffect(() => {
        fetchData();
    }, []);

    const fetchData = async () => {
        try {
            const response = await axios.get('http://localhost:5261/api/stockprice/DDOG');  // Backend API
            console.log('Datadog data:', response.data);  // Logging the data
            setDatadogData([response.data]); // Wrap the response in an array
        } catch (error) {
            console.error("Error fetching Datadog data:", error);
        }
    };

    const fetchComparisonData = async (symbol) => {
        try {
            const response = await axios.get(`http://localhost:5261/api/stockprice/${symbol}`);  // Backend API
            console.log(`${symbol} data:`, response.data);  // Log the data
            if (symbol === 'DT') {
                setDynatraceData([response.data]); // Wrap the response in an array
                setShowDynatrace(true);
            } else if (symbol === 'NEWR') {
                setNewRelicData([response.data]); // Wrap the response in an array
                setShowNewRelic(true);
            }
        } catch (error) {
            console.error(`Error fetching ${symbol} data:`, error);
        }
    };

    return (
        <div>
            <button onClick={() => fetchComparisonData('DT')}>Add Dynatrace</button>
            <button onClick={() => fetchComparisonData('NEWR')}>Add New Relic</button>

            <h2>DDOG Stock Prices</h2>
            <table>
                <thead>
                    <tr>
                        <th>Symbol</th>
                        <th>Price</th>
                        <th>Timestamp</th>
                    </tr>
                </thead>
                <tbody>
                    {datadogData.map((item, index) => (
                        <tr key={index}>
                            <td>{item.symbol}</td>
                            <td>{item.price}</td>
                            <td>{item.timestamp}</td>
                        </tr>
                    ))}
                </tbody>
            </table>

            {showDynatrace && (
                <div>
                    <h2>Dynatrace Stock Prices</h2>
                    <table>
                        <thead>
                            <tr>
                                <th>Symbol</th>
                                <th>Price</th>
                                <th>Timestamp</th>
                            </tr>
                        </thead>
                        <tbody>
                            {dynatraceData.map((item, index) => (
                                <tr key={index}>
                                    <td>{item.symbol}</td>
                                    <td>{item.price}</td>
                                    <td>{item.timestamp}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                    <button onClick={() => setShowDynatrace(false)}>Remove Dynatrace</button>
                </div>
            )}

            {showNewRelic && (
                <div>
                    <h2>New Relic Stock Prices</h2>
                    <table>
                        <thead>
                            <tr>
                                <th>Symbol</th>
                                <th>Price</th>
                                <th>Timestamp</th>
                            </tr>
                        </thead>
                        <tbody>
                            {newRelicData.map((item, index) => (
                                <tr key={index}>
                                    <td>{item.symbol}</td>
                                    <td>{item.price}</td>
                                    <td>{item.timestamp}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                    <button onClick={() => setShowNewRelic(false)}>Remove New Relic</button>
                </div>
            )}
        </div>
    );
};

export default StockPrices;
