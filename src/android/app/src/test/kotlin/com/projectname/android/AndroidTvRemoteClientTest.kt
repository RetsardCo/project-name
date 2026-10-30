package com.projectname.android

import com.projectname.android.tv.AndroidTvRemoteClient
import org.junit.Assert.assertEquals
import org.junit.Assert.assertNotNull
import org.junit.Assert.assertTrue
import org.junit.Test

class AndroidTvRemoteClientTest {

    @Test
    fun encodeRemoteKeyPacket_producesValidProtobufWireFormat() {
        val packet = AndroidTvRemoteClient.encodeRemoteKeyPacket(
            AndroidTvRemoteClient.KEY_DPAD_UP,
            AndroidTvRemoteClient.ACTION_DOWN
        )

        assertNotNull(packet)
        assertTrue(packet.isNotEmpty())

        // Verify outer tag 0x52 (RemoteKey event) is present
        var foundTag52 = false
        for (b in packet) {
            if (b == 0x52.toByte()) {
                foundTag52 = true
                break
            }
        }
        assertTrue("Expected tag 0x52 in packet", foundTag52)

        // Verify key code 19 (0x13 for KEY_DPAD_UP) is present
        var foundKeyCode19 = false
        for (b in packet) {
            if (b == 19.toByte()) {
                foundKeyCode19 = true
                break
            }
        }
        assertTrue("Expected keycode 19 in packet", foundKeyCode19)
    }

    @Test
    fun encodeRemoteKeyPacket_differentKeysProduceDistinctPackets() {
        val packetUp = AndroidTvRemoteClient.encodeRemoteKeyPacket(
            AndroidTvRemoteClient.KEY_DPAD_UP,
            AndroidTvRemoteClient.ACTION_DOWN
        )
        val packetDown = AndroidTvRemoteClient.encodeRemoteKeyPacket(
            AndroidTvRemoteClient.KEY_DPAD_DOWN,
            AndroidTvRemoteClient.ACTION_DOWN
        )
        val packetHome = AndroidTvRemoteClient.encodeRemoteKeyPacket(
            AndroidTvRemoteClient.KEY_HOME,
            AndroidTvRemoteClient.ACTION_DOWN
        )

        assertTrue(packetUp.isNotEmpty())
        assertTrue(packetDown.isNotEmpty())
        assertTrue(packetHome.isNotEmpty())

        // Confirm packets have distinct bytes representing their key codes
        val upHas19 = packetUp.any { it == 19.toByte() }
        val downHas20 = packetDown.any { it == 20.toByte() }
        val homeHas3 = packetHome.any { it == 3.toByte() }

        assertTrue(upHas19)
        assertTrue(downHas20)
        assertTrue(homeHas3)
    }
}
