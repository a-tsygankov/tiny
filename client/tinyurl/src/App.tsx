// src/App.tsx
import React, { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState, AppDispatch } from './store';
import { fetchAllShortUrls } from './urlSlice';
import { UrlStatistics } from './urlSlice';

function App() {
  const dispatch = useDispatch<AppDispatch>();
  const { allStats, loading, error } = useSelector((state: RootState) => state.url);

  useEffect(() => {
    dispatch(fetchAllShortUrls());
  }, [dispatch]);

  // Convert server date/time to local string:
  const toLocal = (dateString: string | null | undefined) => {
    if (!dateString) return '-';
    // e.g. create a Date and format locally:
    const d = new Date(dateString);
    return d.toLocaleString();
  };

  return (
    <div style={{ maxWidth: 600, margin: 'auto' }}>
      <h1>TinyUrl Statistics</h1>
      {loading && <p>Loading...</p>}
      {error && <p style={{ color: 'red' }}>Error: {error}</p>}

      <table style={{ width: '100%', borderCollapse: 'collapse' }}>
        <thead>
          <tr>
            <th>Short URL (Id)</th>
            <th>Click Count</th>
            <th>Last Accessed</th>
            <th>Created At</th>
          </tr>
        </thead>
        <tbody>
          {allStats.map((stats: UrlStatistics) => {
            const shortUrl = stats.id.value; // e.g. "Abc123"
            return (
              <tr key={shortUrl}>
                <td>{shortUrl}</td>
                <td>{stats.clickCount}</td>
                <td>{toLocal(stats.lastAccessed ?? '')}</td>
                <td>{toLocal(stats.createdAt)}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

export default App;
