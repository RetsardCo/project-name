package com.projectname.android.tv

import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.io.ByteArrayOutputStream
import java.io.OutputStream
import java.net.InetSocketAddress
import java.security.SecureRandom
import java.security.cert.X509Certificate
import javax.net.ssl.SSLContext
import javax.net.ssl.SSLSocket
import javax.net.ssl.TrustManager
import javax.net.ssl.X509TrustManager

/**
 * Android client for Google TV / Android TV Remote Service protocol (TLS port 6466).
 * Encodes wire-format protobuf packets for directional D-pad, select, navigation, and volume
 * without requiring ADB at runtime.
 */
class AndroidTvRemoteClient {

    companion object {
        const val DEFAULT_PORT = 6466

        // TV Key Codes
        const val KEY_HOME = 3
        const val KEY_BACK = 4
        const val KEY_DPAD_UP = 19
        const val KEY_DPAD_DOWN = 20
        const val KEY_DPAD_LEFT = 21
        const val KEY_DPAD_RIGHT = 22
        const val KEY_DPAD_CENTER = 23
        const val KEY_VOLUME_UP = 24
        const val KEY_VOLUME_DOWN = 25
        const val KEY_MEDIA_PLAY_PAUSE = 85

        // Actions
        const val ACTION_DOWN = 0
        const val ACTION_UP = 1

        /**
         * Encodes a remote key event into the packet format expected by Android TV Remote Service v2.
         * Outer wire packet: [Outer length varint][tag 0x52][Body length varint][tag 0x08][code varint][tag 0x10][action varint]
         */
        fun encodeRemoteKeyPacket(keyCode: Int, action: Int): ByteArray {
            val body = ByteArrayOutputStream()
            // Field 1: keycode (tag 0x08)
            body.write(0x08)
            writeVarint(body, keyCode)
            // Field 2: action (tag 0x10)
            body.write(0x10)
            writeVarint(body, action)
            val bodyBytes = body.toByteArray()

            val outer = ByteArrayOutputStream()
            outer.write(0x52) // Tag for remote key event
            writeVarint(outer, bodyBytes.size)
            outer.write(bodyBytes)
            val outerBytes = outer.toByteArray()

            val framed = ByteArrayOutputStream()
            writeVarint(framed, outerBytes.size)
            framed.write(outerBytes)
            return framed.toByteArray()
        }

        private fun writeVarint(stream: OutputStream, value: Int) {
            var v = value
            while ((v and 0x7F.inv()) != 0) {
                stream.write((v and 0x7F) or 0x80)
                v = v ushr 7
            }
            stream.write(v and 0x7F)
        }
    }

    private var sslSocket: SSLSocket? = null
    var isConnected: Boolean = false
        private set
    var connectedHost: String? = null
        private set

    /**
     * Asynchronously connects to the target Google TV over TLS.
     */
    suspend fun connect(host: String, port: Int = DEFAULT_PORT): Result<Unit> = withContext(Dispatchers.IO) {
        try {
            disconnect()

            val trustAll = arrayOf<TrustManager>(object : X509TrustManager {
                override fun checkClientTrusted(chain: Array<X509Certificate>?, authType: String?) {}
                override fun checkServerTrusted(chain: Array<X509Certificate>?, authType: String?) {}
                override fun getAcceptedIssuers(): Array<X509Certificate> = arrayOf()
            })

            val sslContext = SSLContext.getInstance("TLS")
            sslContext.init(null, trustAll, SecureRandom())

            val socket = sslContext.socketFactory.createSocket() as SSLSocket
            socket.connect(InetSocketAddress(host, port), 4000)
            socket.soTimeout = 5000
            socket.startHandshake()

            sslSocket = socket
            connectedHost = host
            isConnected = true
            Result.success(Unit)
        } catch (e: Exception) {
            disconnect()
            Result.failure(e)
        }
    }

    /**
     * Sends a click event (DOWN followed by UP) for the specified key code.
     */
    suspend fun sendKeyClick(keyCode: Int): Result<Unit> = withContext(Dispatchers.IO) {
        val socket = sslSocket
        if (socket == null || !isConnected) {
            return@withContext Result.failure(IllegalStateException("Not connected to TV"))
        }

        try {
            val out = socket.outputStream
            val downPacket = encodeRemoteKeyPacket(keyCode, ACTION_DOWN)
            out.write(downPacket)
            out.flush()

            kotlinx.coroutines.delay(50)

            val upPacket = encodeRemoteKeyPacket(keyCode, ACTION_UP)
            out.write(upPacket)
            out.flush()

            Result.success(Unit)
        } catch (e: Exception) {
            disconnect()
            Result.failure(e)
        }
    }

    fun disconnect() {
        try {
            sslSocket?.close()
        } catch (_: Exception) {}
        sslSocket = null
        isConnected = false
        connectedHost = null
    }
}
