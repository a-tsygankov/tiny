// src/urlSlice.ts
import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import axios from 'axios';

type TinyUrlState = {
  allShortUrls: string[];
  loading: boolean;
  error: string | null;
};

const initialState: TinyUrlState = {
  allShortUrls: [],
  loading: false,
  error: null
};

// For demonstration, the base URL of your TinyUrl API
const API_BASE = 'http://localhost:5230';

// 1) Thunk to get all short URLs
export const fetchAllShortUrls = createAsyncThunk(
  'url/fetchAll',
  async (_, { rejectWithValue }) => {
    try {
      const response = await axios.get(`${API_BASE}/tinyurls/all`);
      // Expecting { "shortUrls": [...] }
      return response.data.shortUrls as string[];
    } catch (err: any) {
      return rejectWithValue(err.message);
    }
  }
);

// 2) Thunk to create a short URL
export const createShortUrl = createAsyncThunk(
  'url/create',
  async (
    payload: { longUrl: string; createdBy: string; customAlias?: string },
    { rejectWithValue }
  ) => {
    try {
      const response = await axios.post(`${API_BASE}/tinyurls`, {
        longUrl: payload.longUrl,
        createdBy: payload.createdBy,
        customAlias: payload.customAlias
      });
      // Returns: { "shortUrl": "http://short.ly/alias" } or just the short string
      return response.data.shortUrl || response.data;
    } catch (err: any) {
      return rejectWithValue(err.message);
    }
  }
);

// 3) Thunk to delete
export const deleteShortUrl = createAsyncThunk(
  'url/delete',
  async (shortUrl: string, { rejectWithValue }) => {
    try {
      // ?shortUrl=...
      await axios.delete(`${API_BASE}/tinyurls`, {
        params: { shortUrl }
      });
      return shortUrl;
    } catch (err: any) {
      return rejectWithValue(err.message);
    }
  }
);

const urlSlice = createSlice({
  name: 'url',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      // fetchAllShortUrls
      .addCase(fetchAllShortUrls.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchAllShortUrls.fulfilled, (state, action) => {
        state.loading = false;
        state.allShortUrls = action.payload;
      })
      .addCase(fetchAllShortUrls.rejected, (state, action) => {
        state.loading = false;
        state.error = String(action.payload);
      })
      // createShortUrl
      .addCase(createShortUrl.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(createShortUrl.fulfilled, (state, action) => {
        state.loading = false;
        // The API returns the newly created shortUrl; re-fetch or append
        state.allShortUrls.push(action.payload);
      })
      .addCase(createShortUrl.rejected, (state, action) => {
        state.loading = false;
        state.error = String(action.payload);
      })
      // deleteShortUrl
      .addCase(deleteShortUrl.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(deleteShortUrl.fulfilled, (state, action) => {
        state.loading = false;
        const toDelete = action.payload;
        state.allShortUrls = state.allShortUrls.filter((url) => url !== toDelete);
      })
      .addCase(deleteShortUrl.rejected, (state, action) => {
        state.loading = false;
        state.error = String(action.payload);
      });
  }
});

export default urlSlice.reducer;
