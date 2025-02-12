// src/App.tsx
import React, { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState, AppDispatch } from './store';
import {
  fetchAllShortUrls,
  createShortUrl,
  deleteShortUrl
} from './urlSlice.ts';

function App() {
  const dispatch = useDispatch<AppDispatch>();
  const { allShortUrls, loading, error } = useSelector((state: RootState) => state.url);

  const [longUrl, setLongUrl] = useState('');
  const [createdBy, setCreatedBy] = useState('');
  const [customAlias, setCustomAlias] = useState('');

  useEffect(() => {
    dispatch(fetchAllShortUrls());
  }, [dispatch]);

  const handleCreate = (e: React.FormEvent) => {
    e.preventDefault();
    dispatch(createShortUrl({ longUrl, createdBy, customAlias }));
    setLongUrl('');
    setCreatedBy('');
    setCustomAlias('');
  };

  const handleDelete = (shortUrl: string) => {
    dispatch(deleteShortUrl(shortUrl));
  };

  return (
    <div style={{ maxWidth: 600, margin: 'auto' }}>
      <h1>TinyUrl React App</h1>
      {loading && <p>Loading...</p>}
      {error && <p style={{ color: 'red' }}>Error: {error}</p>}

      <section>
        <h2>All Short URLs</h2>
        <ul>
          {allShortUrls.map((url) => (
            <li key={url}>
              <span>{url}</span>
              {'  '}
              <button onClick={() => handleDelete(url)}>
                Delete
              </button>
            </li>
          ))}
        </ul>
      </section>

      <section style={{ marginTop: '2rem' }}>
        <h2>Create a New Short URL</h2>
        <form onSubmit={handleCreate} style={{ display: 'flex', flexDirection: 'column', maxWidth: 300 }}>
          <label>
            Long URL:
            <input
              type="text"
              value={longUrl}
              onChange={(e) => setLongUrl(e.target.value)}
              required
            />
          </label>
          <label>
            Created By:
            <input
              type="text"
              value={createdBy}
              onChange={(e) => setCreatedBy(e.target.value)}
              required
            />
          </label>
          <label>
            Custom Alias (optional):
            <input
              type="text"
              value={customAlias}
              onChange={(e) => setCustomAlias(e.target.value)}
            />
          </label>
          <button type="submit" style={{ marginTop: '1rem' }}>
            Create
          </button>
        </form>
      </section>
    </div>
  );
}

export default App;
