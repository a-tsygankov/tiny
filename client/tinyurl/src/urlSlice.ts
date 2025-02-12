import { createSlice, createAsyncThunk } from '@reduxjs/toolkit'
import axios from 'axios'

export interface UrlId {
  value: string
  isEmpty: boolean
}
export interface UrlStatistics {
  id: { value: string }
  clickCount: number
  lastAccessed: string | null
  createdAt: string
  longUrl?: string 
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

      await axios.delete(`${API_BASE}/tinyurls`, { params: { shortUrl: `http://short.ly/${shortUrlId}` } })

      const res = await axios.get(`${API_BASE}/tinyurls/all`)
      return res.data as UrlStatistics[]
    } catch (err: any) {
      return rejectWithValue(err.message)
    }
  }
)

export const resolveShortUrl = createAsyncThunk(
  'url/resolve',
  async (shortUrlId: string, { rejectWithValue }) => {
    try {
      const fullShortUrl = `http://short.ly/${shortUrlId}`
      const response = await axios.get(`${API_BASE}/tinyurls/resolve`, {
        params: { shortUrl: fullShortUrl }
      })

      return response.data.longUrl as string
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
