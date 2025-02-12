// src/urlSlice.ts
import { createSlice, createAsyncThunk } from '@reduxjs/toolkit'
import axios from 'axios'

export interface UrlId {
  value: string
  isEmpty: boolean
}

export interface UrlStatistics {
  id: UrlId
  clickCount: number
  lastAccessed: string | null
  createdAt: string
}

interface TinyUrlState {
  allStats: UrlStatistics[]
  loading: boolean
  error: string | null
}

const initialState: TinyUrlState = {
  allStats: [],
  loading: false,
  error: null
}

const API_BASE = 'http://localhost:5230'

export const fetchAllShortUrls = createAsyncThunk(
  'url/fetchAll',
  async (_, { rejectWithValue }) => {
    try {
      const res = await axios.get(`${API_BASE}/tinyurls/all`)
      return res.data as UrlStatistics[]
    } catch (err: any) {
      return rejectWithValue(err.message)
    }
  }
)

export const createShortUrl = createAsyncThunk(
  'url/create',
  async (
    payload: { longUrl: string; createdBy: string; customAlias?: string },
    { rejectWithValue }
  ) => {
    try {
      const res = await axios.post(`${API_BASE}/tinyurls`, {
        longUrl: payload.longUrl,
        createdBy: payload.createdBy,
        customAlias: payload.customAlias
      })
      // Some APIs return { shortUrl: "http://short.ly/alias" }
      // If your API doesn't update the stats right away, you may need to refetch
      // the entire list. We'll do that in the extraReducers below.
      return await axios.get(`${API_BASE}/tinyurls/all`).then(r => r.data as UrlStatistics[])
    } catch (err: any) {
      return rejectWithValue(err.message)
    }
  }
)

export const deleteShortUrl = createAsyncThunk(
  'url/delete',
  async (shortUrlId: string, { rejectWithValue }) => {
    try {
      // Some APIs require the full short URL as a param: '?shortUrl=http://short.ly/xxx'
      // Or just the ID if the service logic supports it. Adjust as needed:
      await axios.delete(`${API_BASE}/tinyurls`, { params: { shortUrl: `http://short.ly/${shortUrlId}` } })
      // After deleting, fetch the updated list
      const res = await axios.get(`${API_BASE}/tinyurls/all`)
      return res.data as UrlStatistics[]
    } catch (err: any) {
      return rejectWithValue(err.message)
    }
  }
)

const urlSlice = createSlice({
  name: 'url',
  initialState,
  reducers: {},
  extraReducers: (builder) => {
    builder
      // fetchAll
      .addCase(fetchAllShortUrls.pending, (state) => {
        state.loading = true
        state.error = null
      })
      .addCase(fetchAllShortUrls.fulfilled, (state, action) => {
        state.loading = false
        state.allStats = action.payload
      })
      .addCase(fetchAllShortUrls.rejected, (state, action) => {
        state.loading = false
        state.error = String(action.payload)
      })
      // createShortUrl
      .addCase(createShortUrl.pending, (state) => {
        state.loading = true
        state.error = null
      })
      .addCase(createShortUrl.fulfilled, (state, action) => {
        state.loading = false
        state.allStats = action.payload // newly fetched list
      })
      .addCase(createShortUrl.rejected, (state, action) => {
        state.loading = false
        state.error = String(action.payload)
      })
      // deleteShortUrl
      .addCase(deleteShortUrl.pending, (state) => {
        state.loading = true
        state.error = null
      })
      .addCase(deleteShortUrl.fulfilled, (state, action) => {
        state.loading = false
        state.allStats = action.payload // newly fetched list
      })
      .addCase(deleteShortUrl.rejected, (state, action) => {
        state.loading = false
        state.error = String(action.payload)
      })
  }
})

export default urlSlice.reducer
