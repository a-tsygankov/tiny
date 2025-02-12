import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import axios from 'axios';

export interface UrlStatistics {
  id: { value: string };
  clickCount: number;
  lastAccessed: string | null;
  createdAt: string;
}

interface TinyUrlState {
  allStats: UrlStatistics[];
  loading: boolean;
  error: string | null;
}

const initialState: TinyUrlState = {
  allStats: [],
  loading: false,
  error: null
};

const API_BASE = 'http://localhost:5230';

export const fetchAllShortUrls = createAsyncThunk(
  'url/fetchAll',
  async (_, { rejectWithValue }) => {
    try {
      const response = await axios.get(`${API_BASE}/tinyurls/all`);
      return response.data as UrlStatistics[];
    } catch (err: any) {
      return rejectWithValue(err.message);
    }
  }
);


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
      .addCase(fetchAllShortUrls.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(fetchAllShortUrls.fulfilled, (state, action) => {
        state.loading = false;
        state.allStats = action.payload;
      })
      .addCase(fetchAllShortUrls.rejected, (state, action) => {
        state.loading = false;
        state.error = String(action.payload);
      });
  }
});

export default urlSlice.reducer;